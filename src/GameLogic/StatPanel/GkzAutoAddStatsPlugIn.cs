// <copyright file="GkzAutoAddStatsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.StatPanel;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Distributes the free stat points automatically on level ups, by the settings of the character's stat panel.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: auto add stats", Description = "Distributes the free stat points automatically on level ups, by the settings of the stat panel.")]
[Guid("6E2B8D41-0C7A-4F35-B9E2-1A5C3D8F6B27")]
public class GkzAutoAddStatsPlugIn : ICharacterLevelUpPlugIn
{
    /// <inheritdoc />
    public void CharacterLeveledUp(Player player)
    {
        if (player.SelectedCharacter is not { } character
            || GkzStatPanel.GetSettings(character) is not { EveryLevels: > 0 } settings
            || (int)player.Attributes![Stats.Level] % settings.EveryLevels != 0)
        {
            return;
        }

        var added = GkzStatPanel.Distribute(player, settings);
        if (added.Count == 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await GkzStatPanel.ShowAddedAsync(player, added).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                player.Logger.LogWarning(ex, "Couldn't show the automatically added stat points.");
            }
        });
    }
}
