// <copyright file="ItemPostHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.ItemPost;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler to post an item of the inventory to the chat (0xEC, 0x50): C1 05 EC 50 [inventory slot].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: item post", Description = "Packet handler to post an item of the inventory to the chat (0xEC, 0x50): C1 05 EC 50 [inventory slot].")]
[Guid("7D2F8B41-0C6E-4A93-A5D1-E83B6F2C49A0")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class ItemPostHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly ItemPostAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x50;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5)
        {
            return;
        }

        await this._action.PostAsync(player, packet.Span[4]).ConfigureAwait(false);
    }
}
