// <copyright file="JewelBankRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the request of the jewel bank (0xEC, 0x30).
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: jewel bank request", Description = "Packet handler for the request of the jewel bank (0xEC, 0x30).")]
[Guid("7B2E8F0A-BCD3-4E47-B081-9CADBECFE0E7")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class JewelBankRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly JewelBankActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x30;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        await this._actions.ShowAsync(player).ConfigureAwait(false);
    }
}
