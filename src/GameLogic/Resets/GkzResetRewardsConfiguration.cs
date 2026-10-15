// <copyright file="GkzResetRewardsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// The configuration of the reset rewards of MU GKZ.
/// </summary>
public class GkzResetRewardsConfiguration
{
    /// <summary>
    /// Gets or sets the milestones: the packages of the item shop which are given into its gift storage.
    /// </summary>
    public List<GkzResetMilestone> Milestones { get; set; } = CreateDefaultMilestones();

    /// <summary>
    /// Gets or sets the first reset of the milestones which repeat (with <see cref="RepeatEvery"/>).
    /// </summary>
    public int RepeatFrom { get; set; } = 125;

    /// <summary>
    /// Gets or sets the length of the repeating block: the milestones from <see cref="RepeatFrom"/> on, for this many resets,
    /// are given again after it, e.g. the ones of 125 to 200 at 225 to 300.
    /// </summary>
    public int RepeatEvery { get; set; } = 100;

    /// <summary>
    /// Gets or sets the last reset with repeated milestones.
    /// </summary>
    public int RepeatUntil { get; set; } = 500;

    /// <summary>
    /// Creates the milestones which were decided for the start of the server.
    /// </summary>
    /// <returns>The milestones.</returns>
    public static List<GkzResetMilestone> CreateDefaultMilestones() =>
    [
        new() { Resets = 10, Packages = [GkzResetRewardPackages.Soul10, GkzResetRewardPackages.Bless10] },
        new() { Resets = 25, ClassWings = 2 },
        new() { Resets = 50, Packages = [GkzResetRewardPackages.Boxes10, GkzResetRewardPackages.Boxes10] },
        new() { Resets = 75, Packages = [GkzResetRewardPackages.Soul20, GkzResetRewardPackages.Bless20, GkzResetRewardPackages.Life20, GkzResetRewardPackages.Chaos20], VipDays = 30 },
        new() { Resets = 100, ClassWings = 3 },
        new() { Resets = 125, Packages = [GkzResetRewardPackages.Boxes10, GkzResetRewardPackages.Boxes10] },
        new() { Resets = 150, Packages = [GkzResetRewardPackages.Boxes10, GkzResetRewardPackages.Boxes5, GkzResetRewardPackages.Soul10, GkzResetRewardPackages.Bless10] },
        new() { Resets = 175, Packages = [GkzResetRewardPackages.Soul20, GkzResetRewardPackages.Bless20] },
        new() { Resets = 200, Packages = [GkzResetRewardPackages.Boxes10, GkzResetRewardPackages.Boxes10, GkzResetRewardPackages.Boxes10] },
    ];
}
