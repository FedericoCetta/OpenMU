// <copyright file="JewelBankDepositHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for a deposit into the jewel bank (0xEC, 0x32): C1 0A EC 32 [group] [number u16] [level] [amount u16, 0 for all].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: jewel bank deposit", Description = "Packet handler for a deposit into the jewel bank (0xEC, 0x32): C1 0A EC 32 [group] [number u16] [level] [amount u16, 0 for all].")]
[Guid("8C3F9A1B-CDE4-4F58-8192-ADBECFE0F1F8")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class JewelBankDepositHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly JewelBankActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x32;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 8)
        {
            return;
        }

        var span = packet.Span;
        var amount = packet.Length >= 10 ? BinaryPrimitives.ReadUInt16LittleEndian(span[8..]) : 0;
        var (group, number, level) = (span[4], BinaryPrimitives.ReadInt16LittleEndian(span[5..]), span[7]);
        await player.RunPersistenceExclusiveAsync(() => this._actions.DepositAsync(player, group, number, level, amount)).ConfigureAwait(false);
    }
}
