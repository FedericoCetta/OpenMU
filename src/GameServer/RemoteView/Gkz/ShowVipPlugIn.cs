// <copyright file="ShowVipPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.GameLogic.Views.Vip;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The implementation of the <see cref="IShowVipPlugIn"/> for the MU GKZ client, with its own packets (0xEC).
/// </summary>
/// <remarks>
/// The layouts must match GkzPackets.h in MuMain. All numbers are little endian.
/// </remarks>
[PlugIn]
[Display(Name = "MU GKZ: VIP menu view", Description = "Sends the VIP menu and the vault selection to the MU GKZ client.")]
[Guid("4E9B5C7D-8FA0-4B14-8D5E-6F7A8B9CADB4")]
public class ShowVipPlugIn : IShowVipPlugIn
{
    /// <summary>
    /// C1 [size] EC 01: coins u32, VIP seconds left u32, price u32, days u16, experience bonus % u16,
    /// item drop bonus in tenths of a percentage point u16, zen bonus % u16, vaults without VIP u8,
    /// vaults with VIP u8, coin name (16 bytes, ASCII, zero padded), Reset Coins u32, price in Reset Coins u32.
    /// </summary>
    internal const int VipInfoLength = 50;

    /// <summary>
    /// C1 [size] EC 02 [result].
    /// </summary>
    internal const int BuyResultLength = 5;

    /// <summary>
    /// C1 [size] EC 10 [open vault] [available vaults].
    /// </summary>
    internal const int VaultSelectionLength = 6;

    /// <summary>
    /// C1 [size] EC 21 [status] [dragged item slot] [target item slot] [price u32] [coins u32].
    /// </summary>
    internal const int CombinationCheckLength = 15;

    /// <summary>
    /// C1 [size] EC 23 [status].
    /// </summary>
    internal const int CombinationResultLength = 5;

    private const byte Code = 0xEC;
    private const int CoinNameLength = 16;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowVipPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowVipPlugIn(RemotePlayer player) => this._player = player;

    /// <summary>
    /// Writes the VIP info packet.
    /// </summary>
    /// <param name="span">The target, at least <see cref="VipInfoLength"/> bytes.</param>
    /// <param name="info">The info.</param>
    /// <param name="now">The current time (UTC).</param>
    internal static void WriteVipInfo(Span<byte> span, VipInfo info, DateTime now)
    {
        var configuration = info.Configuration;
        WriteHeader(span, VipInfoLength, 0x01);
        var secondsLeft = info.VipUntil is { } until && until > now ? (uint)Math.Min(uint.MaxValue, (until - now).TotalSeconds) : 0;
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)Math.Max(0, info.Coins));
        BinaryPrimitives.WriteUInt32LittleEndian(span[8..], secondsLeft);
        BinaryPrimitives.WriteUInt32LittleEndian(span[12..], (uint)Math.Max(0, configuration.Price));
        BinaryPrimitives.WriteUInt16LittleEndian(span[16..], (ushort)Math.Clamp(configuration.DaysPerPurchase, 0, ushort.MaxValue));
        BinaryPrimitives.WriteUInt16LittleEndian(span[18..], ToUShort(configuration.ExperienceBonus * 100));
        BinaryPrimitives.WriteUInt16LittleEndian(span[20..], ToUShort(configuration.ItemDropChanceBonus * 1000));
        BinaryPrimitives.WriteUInt16LittleEndian(span[22..], ToUShort(configuration.MoneyBonus * 100));
        span[24] = (byte)Math.Clamp(configuration.NormalVaults, 1, 6);
        span[25] = (byte)Math.Clamp(configuration.VipVaults, 1, 6);
        var name = span.Slice(26, CoinNameLength);
        name.Clear();
        var ascii = Encoding.ASCII.GetBytes(configuration.CoinName ?? string.Empty);
        ascii.AsSpan(0, Math.Min(ascii.Length, CoinNameLength - 1)).CopyTo(name);
        BinaryPrimitives.WriteUInt32LittleEndian(span[42..], (uint)Math.Max(0, info.ResetCoins));
        BinaryPrimitives.WriteUInt32LittleEndian(span[46..], (uint)Math.Max(0, configuration.ResetCoinsPrice));
    }

    /// <inheritdoc />
    public async ValueTask ShowVipInfoAsync(VipInfo info)
    {
        await this.SendAsync(VipInfoLength, span => WriteVipInfo(span, info, DateTime.UtcNow)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowVipBuyResultAsync(VipBuyResult result)
    {
        await this.SendAsync(BuyResultLength, span =>
        {
            WriteHeader(span, BuyResultLength, 0x02);
            span[4] = (byte)result;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowVaultSelectionAsync(int currentVault, int availableVaults)
    {
        await this.SendAsync(VaultSelectionLength, span =>
        {
            WriteHeader(span, VaultSelectionLength, 0x10);
            span[4] = (byte)currentVault;
            span[5] = (byte)availableVaults;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowItemCombinationCheckAsync(ItemCombinationStatus status, byte sourceSlot, byte targetSlot, int price, int coins)
    {
        await this.SendAsync(CombinationCheckLength, span =>
        {
            WriteHeader(span, CombinationCheckLength, 0x21);
            span[4] = (byte)status;
            span[5] = sourceSlot;
            span[6] = targetSlot;
            BinaryPrimitives.WriteUInt32LittleEndian(span[7..], (uint)Math.Max(0, price));
            BinaryPrimitives.WriteUInt32LittleEndian(span[11..], (uint)Math.Max(0, coins));
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowItemCombinationResultAsync(ItemCombinationStatus status)
    {
        await this.SendAsync(CombinationResultLength, span =>
        {
            WriteHeader(span, CombinationResultLength, 0x23);
            span[4] = (byte)status;
        }).ConfigureAwait(false);
    }

    private static void WriteHeader(Span<byte> span, int length, byte subCode)
    {
        span[0] = 0xC1;
        span[1] = (byte)length;
        span[2] = Code;
        span[3] = subCode;
    }

    private static ushort ToUShort(float value) => (ushort)Math.Clamp(Math.Round(value), 0, ushort.MaxValue);

    private async ValueTask SendAsync(int length, SpanAction write)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var span = connection.Output.GetSpan(length)[..length];
            write(span);
            return length;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }

    private delegate void SpanAction(Span<byte> span);
}
