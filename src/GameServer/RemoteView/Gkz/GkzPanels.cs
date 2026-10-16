// <copyright file="GkzPanels.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Buffers.Binary;
using System.Text;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.StatPanel;
using MUnique.OpenMU.Network;

/// <summary>
/// The packets of the stat panel and of the invasion window of the MU GKZ client.
/// </summary>
/// <remarks>
/// The layouts must match GkzPackets.h in MuMain. All numbers are little endian.
/// </remarks>
internal static class GkzPanels
{
    /// <summary>
    /// C1 0B EC 93 [every levels u16] [strength %] [agility %] [vitality %] [energy %] [command %].
    /// </summary>
    internal const int AutoAddLength = 11;

    /// <summary>
    /// The length of the name of an invasion in the invasion packet.
    /// </summary>
    internal const int InvasionNameLength = 24;

    /// <summary>
    /// The most invasions in one invasion packet.
    /// </summary>
    internal const int MaximumInvasions = 3;

    private const int InvasionHeaderLength = InvasionNameLength + 5;
    private const int MonsterLength = 7;

    /// <summary>
    /// Writes the settings of the automatic stat distribution.
    /// </summary>
    /// <param name="span">The target, at least <see cref="AutoAddLength"/> bytes.</param>
    /// <param name="settings">The settings.</param>
    internal static void WriteAutoAdd(Span<byte> span, GkzAutoAddSettings settings)
    {
        span[0] = 0xC1;
        span[1] = AutoAddLength;
        span[2] = 0xEC;
        span[3] = 0x93;
        BinaryPrimitives.WriteUInt16LittleEndian(span[4..], (ushort)Math.Clamp(settings.EveryLevels, 0, ushort.MaxValue));
        for (var i = 0; i < 5; i++)
        {
            span[6 + i] = i < settings.Percentages.Count ? settings.Percentages[i] : (byte)0;
        }
    }

    /// <summary>
    /// Builds the packet with the running invasions:
    /// C1 [size] EC A0 [count] then for each: [name, 24 bytes] [map u16] [seconds left u16] [monster kinds]
    /// and for each kind: [monster number u16] [left u16] [total u16] [1 if it's the boss].
    /// </summary>
    /// <param name="invasions">The invasions.</param>
    /// <returns>The packet; at most 255 bytes, the monsters which don't fit are left out.</returns>
    internal static byte[] BuildInvasions(IReadOnlyList<GkzInvasionInfo> invasions)
    {
        var packet = new List<byte> { 0xC1, 0, 0xEC, 0xA0, 0 };
        var count = 0;
        foreach (var invasion in invasions.Take(MaximumInvasions))
        {
            var monsters = invasion.Monsters.Take((255 - packet.Count - InvasionHeaderLength) / MonsterLength).ToList();
            if (packet.Count + InvasionHeaderLength > 255)
            {
                break;
            }

            var name = new byte[InvasionNameLength];
            Encoding.UTF8.GetBytes(invasion.Name.Length > InvasionNameLength - 1 ? invasion.Name[..(InvasionNameLength - 1)] : invasion.Name).CopyTo(name, 0);
            packet.AddRange(name);
            AddUInt16(packet, invasion.MapNumber);
            AddUInt16(packet, (ushort)Math.Clamp(invasion.SecondsLeft, 0, ushort.MaxValue));
            packet.Add((byte)monsters.Count);
            foreach (var monster in monsters)
            {
                AddUInt16(packet, monster.MonsterNumber);
                AddUInt16(packet, (ushort)Math.Clamp(monster.Left, 0, ushort.MaxValue));
                AddUInt16(packet, (ushort)Math.Clamp(monster.Total, 0, ushort.MaxValue));
                packet.Add(monster.IsBoss ? (byte)1 : (byte)0);
            }

            count++;
        }

        packet[1] = (byte)packet.Count;
        packet[4] = (byte)count;
        return packet.ToArray();
    }

    /// <summary>
    /// Sends the settings of the automatic stat distribution of the selected character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    internal static ValueTask SendAutoAddAsync(RemotePlayer player)
    {
        if (player.SelectedCharacter is not { } character)
        {
            return ValueTask.CompletedTask;
        }

        var settings = GkzStatPanel.GetSettings(character);
        return SendAsync(player, AutoAddLength, span => WriteAutoAdd(span, settings));
    }

    /// <summary>
    /// Sends the running invasions.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    internal static ValueTask SendInvasionsAsync(RemotePlayer player)
    {
        var packet = BuildInvasions(GkzInvasions.GetRunning(player.GameContext));
        return SendAsync(player, packet.Length, span => packet.CopyTo(span));
    }

    private static void AddUInt16(List<byte> packet, ushort value)
    {
        packet.Add((byte)(value & 0xFF));
        packet.Add((byte)(value >> 8));
    }

    private static async ValueTask SendAsync(RemotePlayer player, int length, SpanAction write)
    {
        if (player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            write(connection.Output.GetSpan(length)[..length]);
            return length;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }

    private delegate void SpanAction(Span<byte> span);
}
