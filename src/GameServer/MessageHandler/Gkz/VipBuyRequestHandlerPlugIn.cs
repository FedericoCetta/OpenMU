// <copyright file="VipBuyRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the VIP purchase (0xEC, 0x02).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: VIP purchase", Description = "Packet handler for the VIP purchase (0xEC, 0x02).")]
[Guid("2C7F3A5B-6D8E-4F92-8B3C-4D5E6F7A8B92")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class VipBuyRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly VipActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x02;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        await this._actions.BuyAsync(player).ConfigureAwait(false);
    }
}
