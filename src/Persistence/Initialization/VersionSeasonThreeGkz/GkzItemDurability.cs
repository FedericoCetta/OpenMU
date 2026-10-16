// <copyright file="GkzItemDurability.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

/// <summary>
/// The equipped items of MU GKZ last <see cref="Factor"/> times longer than in the original game:
/// weapons, armors, wings, rings, pendants and pets lose their durability slower.
/// </summary>
/// <remarks>
/// The original values are set by <see cref="GameConfigurationInitializerBase"/>.
/// They can still be fine-tuned in the game configuration of the admin panel.
/// </remarks>
internal static class GkzItemDurability
{
    /// <summary>
    /// How many times longer the items last.
    /// </summary>
    internal const int Factor = 3;

    /// <summary>
    /// The damage which an armor takes for losing one point of durability. Original: 69.
    /// </summary>
    internal const double DamagePerOneItemDurability = 69 * Factor;

    /// <summary>
    /// The damage which a pet takes for losing one point of durability. Original: 100.
    /// </summary>
    internal const double DamagePerOnePetDurability = 100 * Factor;

    /// <summary>
    /// The hits for which a weapon loses one point of durability. Original: 564.
    /// Wings, rings and pendants decay with time based on this value, too.
    /// </summary>
    internal const double HitsPerOneItemDurability = 564 * Factor;

    /// <summary>
    /// Makes the items of the configuration last longer.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Apply(GameConfiguration gameConfiguration)
    {
        gameConfiguration.DamagePerOneItemDurability = DamagePerOneItemDurability;
        gameConfiguration.DamagePerOnePetDurability = DamagePerOnePetDurability;
        gameConfiguration.HitsPerOneItemDurability = HitsPerOneItemDurability;
    }
}
