// <copyright file="GkzGuardHeartbeatHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.AntiCheat;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the heartbeat of the anti-cheat of the MU GKZ client (0xEC, 0x70):
/// C1 18 EC 70 [sequence u32] [client time u32] [flags u32] [detail u32] [proof u32].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: anti-cheat heartbeat", Description = "Packet handler for the heartbeat of the anti-cheat of the MU GKZ client (0xEC, 0x70).")]
[Guid("A3E9C7B1-6F24-4D8A-9B15-0C7E2F4A8D36")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class GkzGuardHeartbeatHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <summary>
    /// The length of the packet.
    /// </summary>
    internal const int PacketLength = 24;

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x70;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < PacketLength || player.GameContext.FeaturePlugIns.GetPlugIn<GkzGuardPlugIn>() is not { } guard)
        {
            return;
        }

        var span = packet.Span;
        await guard.HandleHeartbeatAsync(
            player,
            BinaryPrimitives.ReadUInt32LittleEndian(span[4..]),
            BinaryPrimitives.ReadUInt32LittleEndian(span[8..]),
            BinaryPrimitives.ReadUInt32LittleEndian(span[12..]),
            BinaryPrimitives.ReadUInt32LittleEndian(span[16..]),
            BinaryPrimitives.ReadUInt32LittleEndian(span[20..])).ConfigureAwait(false);
    }
}
