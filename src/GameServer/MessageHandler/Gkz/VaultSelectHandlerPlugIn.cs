// <copyright file="VaultSelectHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the vault selection (0xEC, 0x10): C1 05 EC 10 [vault number 1-6].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: vault selection", Description = "Packet handler for the vault selection (0xEC, 0x10): C1 05 EC 10 [vault number 1-6].")]
[Guid("3D8A4B6C-7E9F-4A03-9C4D-5E6F7A8B9CA3")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class VaultSelectHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly VipActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x10;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5)
        {
            return;
        }

        await this._actions.SelectVaultAsync(player, packet.Span[4]).ConfigureAwait(false);
    }
}
