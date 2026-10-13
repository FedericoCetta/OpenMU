// <copyright file="JewelBankAutoSaveHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the automatic saving of the jewel bank (0xEC, 0x34): C1 05 EC 34 [0 or 1].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: jewel bank auto save", Description = "Packet handler for the automatic saving of the jewel bank (0xEC, 0x34): C1 05 EC 34 [0 or 1].")]
[Guid("0E5B1C3D-EF06-417A-A3B4-CFE0F1023BBA")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class JewelBankAutoSaveHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly JewelBankActions _actions = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x34;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5)
        {
            return;
        }

        await this._actions.SetAutoSaveAsync(player, packet.Span[4] != 0).ConfigureAwait(false);
    }
}
