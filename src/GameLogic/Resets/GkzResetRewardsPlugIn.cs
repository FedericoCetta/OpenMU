// <copyright file="GkzResetRewardsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The reset rewards of MU GKZ: GKZ Reset Coins for every reset (the Goblin Points of the account, which pay in
/// the item shop and for the VIP), and on milestones items which the player claims in the "Recompensas" category
/// of the item shop, see <see cref="GkzResetRewardClaims"/>.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: reset rewards", Description = "GKZ Reset Coins for every reset, and items on milestones, which are claimed in the item shop.")]
[Guid("D41A7C29-8E3B-4F65-B2D0-6A9E1C5F7B38")]
public class GkzResetRewardsPlugIn : IFeaturePlugIn, IPlayerResetPlugIn, ISupportCustomConfiguration<GkzResetRewardsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public GkzResetRewardsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the shop packages of the milestone of the reset count, if there is one.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="resetCount">The number of resets.</param>
    /// <param name="classIndex">The index of the class (0 DW, 1 DK, 2 Elf, 3 MG, 4 DL).</param>
    /// <returns>The sequence numbers of the packages.</returns>
    public static IReadOnlyList<int> GetMilestonePackages(GkzResetRewardsConfiguration configuration, int resetCount, int classIndex)
    {
        var milestoneReset = resetCount;
        if (resetCount >= configuration.RepeatFrom + configuration.RepeatEvery && resetCount <= configuration.RepeatUntil && configuration.RepeatEvery > 0)
        {
            // 225 -> 125, 250 -> 150, ... 300 -> 200, 325 -> 125 ...
            milestoneReset = configuration.RepeatFrom + ((resetCount - configuration.RepeatFrom) % configuration.RepeatEvery);
        }

        if (configuration.Milestones.FirstOrDefault(m => m.Resets == milestoneReset) is not { } milestone)
        {
            return [];
        }

        var packages = milestone.Packages.ToList();
        if (milestone.ClassWings is 2 or 3)
        {
            packages.Add((milestone.ClassWings == 2 ? GkzResetRewardPackages.SecondWings : GkzResetRewardPackages.ThirdWings) + classIndex);
        }

        return packages;
    }

    /// <summary>
    /// Gets the index of the class for the class specific rewards.
    /// </summary>
    /// <param name="classNumber">The number of the character class.</param>
    /// <returns>0 DW, 1 DK, 2 Elf, 3 MG, 4 DL.</returns>
    public static int GetClassIndex(byte classNumber) => classNumber switch
    {
        <= 3 => 0,
        <= 7 => 1,
        <= 11 => 2,
        <= 13 => 3,
        _ => 4,
    };

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GkzResetRewardsConfiguration();

    /// <inheritdoc />
    public async ValueTask PlayerResetAsync(Player player, int resetCount)
    {
        if (player.Account is not { } account)
        {
            return;
        }

        var configuration = this.Configuration ??= new GkzResetRewardsConfiguration();
        var coins = configuration.CoinsPerReset;
        if (configuration.BonusEveryResets > 0 && resetCount % configuration.BonusEveryResets == 0)
        {
            coins += configuration.BonusCoins;
        }

        account.SetCashShopCoins(DataModel.Configuration.CashShopCoinType.GoblinPoints, account.GoblinPoints + coins);

        player.Logger.LogInformation("Player {player} did reset {resets}: {coins} Reset Coins.", player, resetCount, coins);
        await player.ShowBlueMessageAsync($"Reset {resetCount}: +{coins} Reset Coins (total {account.GoblinPoints}).").ConfigureAwait(false);
        if (GetMilestonePackages(configuration, resetCount, 0).Count > 0)
        {
            await player.ShowBlueMessageAsync($"¡Llegaste a {resetCount} resets! Canjeá tu premio en la tienda (tecla X), en Recompensas.").ConfigureAwait(false);
        }
    }
}
