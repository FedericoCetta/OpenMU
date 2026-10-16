// <copyright file="GkzLevelUpPoints.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Buffers.Binary;
using MUnique.OpenMU.Network;

/// <summary>
/// The free stat points of MU GKZ, which can be more than the 65535 of the usual packets (a reset gives the points
/// of all resets at once): C1 08 EC 80 [points u32]. The MU GKZ client keeps them in 32 bits.
/// </summary>
/// <remarks>
/// The layout must match GkzPackets.h in MuMain. The usual packets send at most 65535, so that older clients show
/// the maximum instead of a wrapped around number.
/// </remarks>
internal static class GkzLevelUpPoints
{
    /// <summary>
    /// The length of the packet.
    /// </summary>
    internal const int Length = 8;

    /// <summary>
    /// Gets the free points for the usual 16 bit fields: at most 65535.
    /// </summary>
    /// <param name="points">The free points.</param>
    /// <returns>The points, between 0 and 65535.</returns>
    internal static ushort Clamp(int points) => (ushort)Math.Clamp(points, 0, ushort.MaxValue);

    /// <summary>
    /// Writes the packet.
    /// </summary>
    /// <param name="span">The target, at least <see cref="Length"/> bytes.</param>
    /// <param name="points">The free points.</param>
    internal static void Write(Span<byte> span, int points)
    {
        span[0] = 0xC1;
        span[1] = Length;
        span[2] = 0xEC;
        span[3] = 0x80;
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)Math.Max(0, points));
    }

    /// <summary>
    /// Sends the free points of the selected character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    internal static async ValueTask SendAsync(RemotePlayer player)
    {
        if (player.Connection is not { } connection || player.SelectedCharacter is not { } character)
        {
            return;
        }

        var points = character.LevelUpPoints;

        int WritePacket()
        {
            Write(connection.Output.GetSpan(Length)[..Length], points);
            return Length;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }
}
