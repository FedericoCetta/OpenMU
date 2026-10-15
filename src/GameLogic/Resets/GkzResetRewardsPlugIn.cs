// <copyright file="GkzResetRewardsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The reset rewards of MU GKZ: GKZ Reset Coins for every reset (the Goblin Points of the account, which pay in
/// the item shop and for the VIP), and on milestones items into the gift storage of the item shop, from where the
/// player takes them when there's space in the inventory.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: reset rewards", Description = "GKZ Reset Coins for every reset, and items into the gift storage of the item shop on milestones.")]
[Guid("D41A7C29-8E3B-4F65-B2D0-6A9E1C5F7B38")]
public class GkzResetRewardsPlugIn : IPlayerResetPlugIn, ISupportCustomConfiguration<GkzResetRewardsConfiguration>, ISupportDefaultCustomConfiguration
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
        if (player.Account is not { } account || player.SelectedCharacter?.CharacterClass is not { } characterClass)
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

        var packages = GetMilestonePackages(configuration, resetCount, GetClassIndex(characterClass.Number));
        var shop = player.GameContext.Configuration.CashShopConfiguration;
        var given = 0;
        foreach (var sequence in packages)
        {
            if (shop?.Packages.FirstOrDefault(p => p.PackageSequence == sequence)?.Products.FirstOrDefault() is not { } product)
            {
                player.Logger.LogWarning("Reset reward package {sequence} for {resets} resets isn't in the item shop.", sequence, resetCount);
                continue;
            }

            var gift = player.PersistenceContext.CreateNew<CashShopStorageItem>();
            gift.AccountId = account.GetId();
            gift.ProductSequence = product.ProductSequence;
            gift.PriceSequence = product.PriceSequence;
            gift.IsGift = true;
            gift.GiftSenderName = configuration.SenderName;
            gift.GiftMessage = $"Premio por {resetCount} resets";
            gift.AddedAt = DateTime.UtcNow;
            given++;
        }

        player.Logger.LogInformation("Player {player} did reset {resets}: {coins} Reset Coins, {gifts} reward(s).", player, resetCount, coins, given);
        await player.ShowBlueMessageAsync($"Reset {resetCount}: +{coins} Reset Coins (total {account.GoblinPoints}).").ConfigureAwait(false);
        if (given > 0)
        {
            await player.ShowBlueMessageAsync("¡Premio por resets! Retiralo del depósito de regalos de la tienda (tecla X).").ConfigureAwait(false);
        }
    }
}
