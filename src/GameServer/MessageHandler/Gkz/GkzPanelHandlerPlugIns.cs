// <copyright file="GkzPanelHandlerPlugIns.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Character;
using MUnique.OpenMU.GameLogic.StatPanel;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.GameServer.RemoteView.Gkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler to add points to several stats at once (0xEC, 0x90):
/// C1 0E EC 90 [strength u16] [agility u16] [vitality u16] [energy u16] [command u16].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: add stats", Description = "Packet handler to add points to several stats at once (0xEC, 0x90).")]
[Guid("C2A84E61-7F3B-4D19-8E05-B6D17A3F9C40")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class AddStatsHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly IncreaseStatsAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x90;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 14 || player.SelectedCharacter is not { CharacterClass: { } characterClass })
        {
            return;
        }

        for (var i = 0; i < GkzStatPanel.StatsInOrder.Count; i++)
        {
            var amount = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[(4 + (i * 2))..]);
            var stat = GkzStatPanel.StatsInOrder[i];
            if (amount == 0 || characterClass.GetStatAttribute(stat) is not { IncreasableByPlayer: true })
            {
                continue;
            }

            amount = (ushort)Math.Min(amount, Math.Max(0, player.SelectedCharacter.LevelUpPoints));
            if (amount > 0)
            {
                await this._action.IncreaseStatsAsync(player, stat, amount).ConfigureAwait(false);
            }
        }
    }
}

/// <summary>
/// Packet handler which asks for the settings of the automatic stat distribution (0xEC, 0x91): C1 04 EC 91.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: auto add stats request", Description = "Packet handler which asks for the settings of the automatic stat distribution (0xEC, 0x91).")]
[Guid("C2A84E61-7F3B-4D19-8E05-B6D17A3F9C41")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class AutoAddStatsRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x91;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is RemotePlayer remotePlayer)
        {
            await GkzPanels.SendAutoAddAsync(remotePlayer).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Packet handler which saves the settings of the automatic stat distribution (0xEC, 0x92):
/// C1 0B EC 92 [every levels u16, 0 is off] [strength %] [agility %] [vitality %] [energy %] [command %].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: auto add stats save", Description = "Packet handler which saves the settings of the automatic stat distribution (0xEC, 0x92).")]
[Guid("C2A84E61-7F3B-4D19-8E05-B6D17A3F9C42")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class AutoAddStatsSaveHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0x92;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 11)
        {
            return;
        }

        var everyLevels = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[4..]);
        var settings = new GkzAutoAddSettings(everyLevels, packet.Span[6..11].ToArray());
        var saved = false;
        await player.RunPersistenceExclusiveAsync(async () =>
        {
            saved = GkzStatPanel.TrySetSettings(player, settings) && await player.SaveProgressAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        await player.ShowBlueMessageAsync(saved
            ? (everyLevels > 0 ? $"Auto Add guardado: se reparten tus puntos cada {everyLevels} niveles." : "Auto Add desactivado.")
            : "No se pudo guardar el Auto Add: los porcentajes no pueden sumar más de 100.").ConfigureAwait(false);
        if (player is RemotePlayer remotePlayer)
        {
            await GkzPanels.SendAutoAddAsync(remotePlayer).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Packet handler which asks for the running invasions (0xEC, 0xA1): C1 04 EC A1.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: invasions request", Description = "Packet handler which asks for the running invasions (0xEC, 0xA1).")]
[Guid("C2A84E61-7F3B-4D19-8E05-B6D17A3F9C43")]
[BelongsToGroup(GkzGroupPlugIn.GroupKey)]
internal class InvasionsRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => 0xA1;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is RemotePlayer remotePlayer)
        {
            await GkzPanels.SendInvasionsAsync(remotePlayer).ConfigureAwait(false);
        }
    }
}
