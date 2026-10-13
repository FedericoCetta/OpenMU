// <copyright file="SplitStackHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler to take one piece off a stack (0xEC, 0x40): C1 05 EC 40 [inventory slot].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: split stack", Description = "Packet handler to take one piece off a stack (0xEC, 0x40): C1 05 EC 40 [inventory slot].")]
[Guid("3E6B2D94-7A1C-4F05-9B8E-5C2A71D4F0B3")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class SplitStackHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly SplitStackAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x40;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5)
        {
            return;
        }

        await this._action.SplitAsync(player, packet.Span[4]).ConfigureAwait(false);
    }
}
