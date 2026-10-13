// <copyright file="ItemCombinationHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for an item combination (0xEC, 0x22): C1 06 EC 22 [dragged item slot] [target item slot].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: item combination", Description = "Packet handler for an item combination (0xEC, 0x22): C1 06 EC 22 [dragged item slot] [target item slot].")]
[Guid("6A1D7E9F-ABC2-4D36-AF70-8B9CADBECFD6")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class ItemCombinationHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly ItemCombinationAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x22;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 6)
        {
            return;
        }

        await this._action.CombineAsync(player, packet.Span[4], packet.Span[5]).ConfigureAwait(false);
    }
}
