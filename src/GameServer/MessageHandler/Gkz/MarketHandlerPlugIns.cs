// <copyright file="MarketHandlerPlugIns.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Market;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The packets of the market of MU GKZ (O key). All are C1 EC [sub code] [payload].
/// </summary>
internal static class MarketPackets
{
    /// <summary>Shows a page of the market: [page u16] [category] [excellent only] [minimum level] [name length] [name].</summary>
    internal const byte Page = 0x61;

    /// <summary>Shows the own store.</summary>
    internal const byte Store = 0x62;

    /// <summary>Opens (pays) the own store.</summary>
    internal const byte OpenStore = 0x63;

    /// <summary>Puts an item for sale: [inventory slot] [currency] [price u32].</summary>
    internal const byte Sell = 0x64;

    /// <summary>Buys an item: [listing id, 16 bytes].</summary>
    internal const byte Buy = 0x65;

    /// <summary>Takes an own item back: [listing id, 16 bytes].</summary>
    internal const byte Withdraw = 0x66;

    /// <summary>Collects the zen and coins of the sales.</summary>
    internal const byte Collect = 0x67;

    /// <summary>The longest name filter.</summary>
    internal const int MaximumNameLength = 20;

    /// <summary>
    /// Runs a market action when the market is active and the player is in a safe zone, like the client requires.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="action">The action.</param>
    internal static async ValueTask RunAsync(Player player, Func<MarketService, ValueTask> action)
    {
        if (MarketPlugIn.GetService(player.GameContext) is not { } market
            || player.PlayerState.CurrentState != PlayerState.EnteredWorld
            || !player.IsAtSafezone())
        {
            return;
        }

        // Under the persistence lock of the player, so a periodic save never sees a half done market action.
        await player.RunPersistenceExclusiveAsync(() => action(market)).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a listing id.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <returns>The id; <c>null</c>, if the packet is too short.</returns>
    internal static Guid? ReadListingId(Memory<byte> packet)
    {
        return packet.Length >= 20 ? new Guid(packet.Span.Slice(4, 16)) : null;
    }
}

/// <summary>
/// Packet handler which shows a page of the market (0xEC, 0x61).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market page", Description = "Packet handler which shows a page of the market (0xEC, 0x61).")]
[Guid("A1C4E8F2-3B57-4D9A-8E26-7F0B5C1D3E94")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketPageHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.Page;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        var filter = MarketFilter.All;
        var page = 0;
        if (packet.Length >= 10)
        {
            var span = packet.Span;
            page = BinaryPrimitives.ReadUInt16LittleEndian(span[4..]);
            var nameLength = Math.Min(Math.Min((int)span[9], MarketPackets.MaximumNameLength), packet.Length - 10);
            var name = nameLength > 0 ? Encoding.UTF8.GetString(span.Slice(10, nameLength)) : string.Empty;
            var category = Enum.IsDefined((MarketCategory)span[6]) ? (MarketCategory)span[6] : MarketCategory.All;
            filter = new MarketFilter(name, category, span[7] != 0, Math.Min(span[8], (byte)15));
        }

        return MarketPackets.RunAsync(player, market => market.ShowPageAsync(player, filter, page));
    }
}

/// <summary>
/// Packet handler which shows the own market store (0xEC, 0x62).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market store", Description = "Packet handler which shows the own market store (0xEC, 0x62).")]
[Guid("B2D5F903-4C68-4EAB-9F37-80C16D2E4FA5")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketStoreHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.Store;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        return MarketPackets.RunAsync(player, market => market.ShowStoreAsync(player));
    }
}

/// <summary>
/// Packet handler which opens (pays) the own market store (0xEC, 0x63).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: open market store", Description = "Packet handler which opens (pays) the own market store (0xEC, 0x63).")]
[Guid("C3E6A014-5D79-4FBC-A048-91D27E3F50B6")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketOpenStoreHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.OpenStore;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        return MarketPackets.RunAsync(player, async market => await market.OpenStoreAsync(player).ConfigureAwait(false));
    }
}

/// <summary>
/// Packet handler which puts an item for sale (0xEC, 0x64): [inventory slot] [currency] [price u32].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market sell", Description = "Packet handler which puts an item for sale (0xEC, 0x64).")]
[Guid("D4F7B125-6E8A-40CD-B159-A2E38F4061C7")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketSellHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.Sell;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 10)
        {
            return ValueTask.CompletedTask;
        }

        var span = packet.Span;
        var slot = span[4];
        var currency = (MarketCurrency)span[5];
        var price = BinaryPrimitives.ReadUInt32LittleEndian(span[6..]);
        return MarketPackets.RunAsync(player, async market => await market.SellAsync(player, slot, price, currency).ConfigureAwait(false));
    }
}

/// <summary>
/// Packet handler which buys an item of the market (0xEC, 0x65): [listing id].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market buy", Description = "Packet handler which buys an item of the market (0xEC, 0x65).")]
[Guid("E5081236-7F9B-41DE-826A-B3F4905172D8")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketBuyHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.Buy;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        return MarketPackets.ReadListingId(packet) is { } listingId
            ? MarketPackets.RunAsync(player, async market => await market.BuyAsync(player, listingId).ConfigureAwait(false))
            : ValueTask.CompletedTask;
    }
}

/// <summary>
/// Packet handler which takes an own item back from the market (0xEC, 0x66): [listing id].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market withdraw", Description = "Packet handler which takes an own item back from the market (0xEC, 0x66).")]
[Guid("F6192347-80AC-42EF-937B-C4051A6283E9")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketWithdrawHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.Withdraw;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        return MarketPackets.ReadListingId(packet) is { } listingId
            ? MarketPackets.RunAsync(player, async market => await market.WithdrawAsync(player, listingId).ConfigureAwait(false))
            : ValueTask.CompletedTask;
    }
}

/// <summary>
/// Packet handler which collects the zen and coins of the sales (0xEC, 0x67).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market collect", Description = "Packet handler which collects the zen and coins of the sales (0xEC, 0x67).")]
[Guid("07A23458-91BD-43F0-A48C-D5162B7394FA")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class MarketCollectHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => MarketPackets.Collect;

    /// <inheritdoc/>
    public ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        return MarketPackets.RunAsync(player, async market => await market.CollectAsync(player).ConfigureAwait(false));
    }
}
