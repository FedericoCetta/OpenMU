// <copyright file="VipInfoRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the request of the VIP menu (0xEC, 0x01).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: VIP menu request", Description = "Packet handler for the request of the VIP menu (0xEC, 0x01).")]
[Guid("1B6E2F4A-5C7D-4E81-9A2B-3C4D5E6F7A81")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class VipInfoRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly VipActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x01;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        await this._actions.ShowInfoAsync(player).ConfigureAwait(false);
    }
}
