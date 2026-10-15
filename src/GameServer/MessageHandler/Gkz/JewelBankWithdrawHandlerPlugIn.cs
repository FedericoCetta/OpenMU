// <copyright file="JewelBankWithdrawHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for a withdrawal from the jewel bank (0xEC, 0x33): C1 0A EC 33 [group] [number u16] [level] [amount u16].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: jewel bank withdrawal", Description = "Packet handler for a withdrawal from the jewel bank (0xEC, 0x33): C1 0A EC 33 [group] [number u16] [level] [amount u16].")]
[Guid("9D4A0B2C-DEF5-4069-92A3-BECFE0F102A9")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class JewelBankWithdrawHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly JewelBankActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x33;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 10)
        {
            return;
        }

        var span = packet.Span;
        var (group, number, level, amount) = (span[4], BinaryPrimitives.ReadInt16LittleEndian(span[5..]), span[7], BinaryPrimitives.ReadUInt16LittleEndian(span[8..]));
        await player.RunPersistenceExclusiveAsync(() => this._actions.WithdrawAsync(player, group, number, level, amount)).ConfigureAwait(false);
    }
}
