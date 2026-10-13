// <copyright file="ItemCombinationCheckHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the check of an item combination (0xEC, 0x20): C1 06 EC 20 [dragged item slot] [target item slot].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: item combination check", Description = "Packet handler for the check of an item combination (0xEC, 0x20): C1 06 EC 20 [dragged item slot] [target item slot].")]
[Guid("5F0C6D8E-9AB1-4C25-9E6F-7A8B9CADBEC5")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class ItemCombinationCheckHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly ItemCombinationAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x20;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 6)
        {
            return;
        }

        await this._action.CheckAsync(player, packet.Span[4], packet.Span[5]).ConfigureAwait(false);
    }
}
