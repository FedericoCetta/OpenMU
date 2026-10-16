// <copyright file="GkzStatPanel.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.StatPanel;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Character;

/// <summary>
/// The settings of the automatic distribution of the free stat points of a character.
/// </summary>
/// <param name="EveryLevels">Every how many levels the points are distributed; 0 when it's off.</param>
/// <param name="Percentages">The percentage of the free points for strength, agility, vitality, energy and command.</param>
public record GkzAutoAddSettings(int EveryLevels, IReadOnlyList<byte> Percentages);

/// <summary>
/// The stat panel of MU GKZ: adds many points at once, and distributes the free points automatically on level ups
/// (see <see cref="GkzAutoAddStatsPlugIn"/>). The settings are kept in stat attributes of the character.
/// </summary>
public static class GkzStatPanel
{
    /// <summary>
    /// Gets the attribute with every how many levels the points are distributed; 0 when it's off.
    /// </summary>
    public static AttributeDefinition EveryLevelsAttribute { get; } = new(new Guid("A3E7C1D2-5B48-4F9A-9C61-0D2E8F7B4A10"), "GKZ auto add: every levels", "The automatic distribution of the free stat points: every how many levels; 0 is off.");

    /// <summary>
    /// Gets the attributes with the percentages of strength, agility, vitality, energy and command.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> PercentageAttributes { get; } =
    [
        new(new Guid("A3E7C1D2-5B48-4F9A-9C61-0D2E8F7B4A11"), "GKZ auto add: strength %", "The percentage of the free stat points which go to strength."),
        new(new Guid("A3E7C1D2-5B48-4F9A-9C61-0D2E8F7B4A12"), "GKZ auto add: agility %", "The percentage of the free stat points which go to agility."),
        new(new Guid("A3E7C1D2-5B48-4F9A-9C61-0D2E8F7B4A13"), "GKZ auto add: vitality %", "The percentage of the free stat points which go to vitality."),
        new(new Guid("A3E7C1D2-5B48-4F9A-9C61-0D2E8F7B4A14"), "GKZ auto add: energy %", "The percentage of the free stat points which go to energy."),
        new(new Guid("A3E7C1D2-5B48-4F9A-9C61-0D2E8F7B4A15"), "GKZ auto add: command %", "The percentage of the free stat points which go to command."),
    ];

    /// <summary>
    /// Gets the stats in the order of the percentages and of the packets: strength, agility, vitality, energy, command.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> StatsInOrder { get; } =
        [Stats.BaseStrength, Stats.BaseAgility, Stats.BaseVitality, Stats.BaseEnergy, Stats.BaseLeadership];

    /// <summary>
    /// Gets all attributes which keep the settings.
    /// </summary>
    public static IEnumerable<AttributeDefinition> SettingAttributes => PercentageAttributes.Prepend(EveryLevelsAttribute);

    /// <summary>
    /// Gets the settings of the character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>The settings.</returns>
    public static GkzAutoAddSettings GetSettings(Character character)
    {
        int Read(AttributeDefinition definition)
            => (int)(character.Attributes.FirstOrDefault(a => a.Definition?.Id == definition.Id)?.Value ?? 0);

        return new GkzAutoAddSettings(Read(EveryLevelsAttribute), PercentageAttributes.Select(a => (byte)Math.Clamp(Read(a), 0, 100)).ToList());
    }

    /// <summary>
    /// Saves the settings into the character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="settings">The settings.</param>
    /// <returns><c>true</c> when they are valid and saved.</returns>
    public static bool TrySetSettings(Player player, GkzAutoAddSettings settings)
    {
        if (player.SelectedCharacter is not { } character
            || settings.EveryLevels is < 0 or > 400
            || settings.Percentages.Count != PercentageAttributes.Count
            || settings.Percentages.Sum(p => p) > 100)
        {
            return false;
        }

        var values = PercentageAttributes.Zip(settings.Percentages, (a, p) => (Definition: a, Value: (float)p))
            .Prepend((EveryLevelsAttribute, settings.EveryLevels));
        foreach (var (definition, value) in values)
        {
            if (player.GameContext.Configuration.Attributes.FirstOrDefault(a => a.Id == definition.Id) is not { } configured)
            {
                return false;
            }

            if (character.Attributes.FirstOrDefault(a => a.Definition?.Id == definition.Id) is { } attribute)
            {
                attribute.Value = value;
            }
            else
            {
                character.Attributes.Add(player.PersistenceContext.CreateNew<StatAttribute>(configured, value));
            }
        }

        return true;
    }

    /// <summary>
    /// Distributes the free points of the character by the percentages, without going over the maximum of a stat.
    /// What doesn't fit stays free.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The added points by stat.</returns>
    public static IReadOnlyList<(AttributeDefinition Stat, ushort Amount)> Distribute(Player player, GkzAutoAddSettings settings)
    {
        if (player.SelectedCharacter is not { CharacterClass: { } characterClass } character || player.Attributes is not { } attributes)
        {
            return [];
        }

        var total = character.LevelUpPoints;
        var freePoints = total;
        var added = new List<(AttributeDefinition, ushort)>();
        for (var i = 0; i < StatsInOrder.Count && freePoints > 0; i++)
        {
            if (characterClass.GetStatAttribute(StatsInOrder[i]) is not { IncreasableByPlayer: true, Attribute: { } stat })
            {
                continue;
            }

            var amount = (int)((long)total * settings.Percentages[i] / 100);
            if (stat.MaximumValue is { } maximum)
            {
                amount = Math.Min(amount, (int)(maximum - attributes[stat]));
            }

            amount = Math.Clamp(Math.Min(amount, freePoints), 0, ushort.MaxValue);
            if (amount == 0)
            {
                continue;
            }

            attributes[stat] += amount;
            freePoints -= amount;
            added.Add((stat, (ushort)amount));
        }

        character.LevelUpPoints = freePoints;
        return added;
    }

    /// <summary>
    /// Tells the client about the added points.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="added">The added points by stat.</param>
    /// <returns>The task.</returns>
    public static async ValueTask ShowAddedAsync(Player player, IReadOnlyList<(AttributeDefinition Stat, ushort Amount)> added)
    {
        foreach (var (stat, amount) in added)
        {
            await player.InvokeViewPlugInAsync<IStatIncreaseResultPlugIn>(p => p.StatIncreaseResultAsync(stat, amount)).ConfigureAwait(false);
        }
    }
}
