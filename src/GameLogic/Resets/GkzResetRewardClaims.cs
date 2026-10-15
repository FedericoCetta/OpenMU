// <copyright file="GkzResetRewardClaims.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.GameLogic.Vip;
using MUnique.OpenMU.Persistence;

/// <summary>
/// The claims of the reset rewards of MU GKZ, like a battle pass: the player claims the reward of a milestone in
/// the category "Reset" of the item shop, once per milestone which the character reached, and gets its items into
/// the storage of the shop. The claimed milestones are a bit mask in a stat attribute of the character.
/// </summary>
public static class GkzResetRewardClaims
{
    /// <summary>
    /// Gets the attribute with the claimed milestones of the character, as bit mask over <see cref="GetAllOccurrences"/>.
    /// </summary>
    public static AttributeDefinition ClaimedAttribute { get; } = new(new Guid("6C1F0E2B-93D4-4A7E-8B25-F04D7A3C9E61"), "GKZ claimed reset rewards", "The reset rewards which the character claimed in the item shop, as bit mask.");

    /// <summary>
    /// Determines whether the package of the shop is the claim of a reset reward.
    /// </summary>
    /// <param name="packageSequence">The package sequence.</param>
    /// <returns><c>true</c> if it is.</returns>
    public static bool IsClaimPackage(int packageSequence) => packageSequence is >= GkzResetRewardPackages.FirstClaim and <= GkzResetRewardPackages.LastClaim;

    /// <summary>
    /// Gets the resets at which the milestone is reached: once, or again in every repetition.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="milestone">The milestone.</param>
    /// <returns>The resets, ascending.</returns>
    public static IEnumerable<int> GetOccurrences(GkzResetRewardsConfiguration configuration, GkzResetMilestone milestone)
    {
        yield return milestone.Resets;
        if (configuration.RepeatEvery <= 0 || milestone.Resets < configuration.RepeatFrom)
        {
            yield break;
        }

        for (var resets = milestone.Resets + configuration.RepeatEvery; resets <= configuration.RepeatUntil; resets += configuration.RepeatEvery)
        {
            yield return resets;
        }
    }

    /// <summary>
    /// Gets the resets of all milestones, ascending; the index is the bit of the claim.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The resets.</returns>
    public static List<int> GetAllOccurrences(GkzResetRewardsConfiguration configuration)
    {
        return configuration.Milestones.SelectMany(m => GetOccurrences(configuration, m)).Distinct().Order().ToList();
    }

    /// <summary>
    /// Gets the first occurrence of the milestone which the character reached and didn't claim yet.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="milestone">The milestone.</param>
    /// <param name="resets">The resets of the character.</param>
    /// <param name="claimed">The bit mask of the claimed occurrences.</param>
    /// <returns>The resets of the occurrence; 0 if there's none.</returns>
    public static int GetClaimable(GkzResetRewardsConfiguration configuration, GkzResetMilestone milestone, int resets, int claimed)
    {
        var all = GetAllOccurrences(configuration);
        return GetOccurrences(configuration, milestone).FirstOrDefault(o => o <= resets && (claimed & (1 << all.IndexOf(o))) == 0);
    }

    /// <summary>
    /// Claims the reward of the milestone of the package, if the character reached it and didn't claim it yet.
    /// The items go into the storage of the item shop.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="shop">The shop configuration.</param>
    /// <param name="packageSequence">The package sequence of the claim.</param>
    /// <returns>The result.</returns>
    public static async ValueTask<CashShopBuyResult> ClaimAsync(Player player, CashShopConfiguration shop, int packageSequence)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<GkzResetRewardsPlugIn>() is not { } plugIn
            || player.Account is not { } account
            || player.SelectedCharacter is not { CharacterClass: { } characterClass } character
            || player.GameContext.Configuration.Attributes.FirstOrDefault(a => a == ClaimedAttribute) is not { } definition)
        {
            return CashShopBuyResult.NotForSale;
        }

        var configuration = plugIn.Configuration ?? new GkzResetRewardsConfiguration();
        var index = packageSequence - GkzResetRewardPackages.FirstClaim;
        if (index < 0 || index >= configuration.Milestones.Count)
        {
            return CashShopBuyResult.PackageNotFound;
        }

        var milestone = configuration.Milestones[index];
        var allOccurrences = GetAllOccurrences(configuration);
        string? message = null;
        var result = await player.RunPersistenceExclusiveAsync(async () =>
        {
            var resets = (int)(player.Attributes?[Stats.Resets] ?? 0);
            var attribute = character.Attributes.FirstOrDefault(a => a.Definition == definition);
            var claimed = (int)(attribute?.Value ?? 0);
            var claimable = GetClaimable(configuration, milestone, resets, claimed);
            if (claimable == 0)
            {
                var next = GetOccurrences(configuration, milestone).FirstOrDefault(o => o > resets);
                message = next > 0
                    ? $"Este premio es a los {next} resets: te faltan {next - resets}."
                    : "Ya canjeaste este premio.";
                return CashShopBuyResult.RefusedByPlugIn;
            }

            var products = new List<CashShopProduct>();
            foreach (var sequence in GkzResetRewardsPlugIn.GetMilestonePackages(configuration, claimable, GkzResetRewardsPlugIn.GetClassIndex(characterClass.Number)))
            {
                if (shop.Packages.FirstOrDefault(p => p.PackageSequence == sequence)?.Products.FirstOrDefault() is not { } product)
                {
                    player.Logger.LogError("Reset reward package {sequence} for {resets} resets isn't in the item shop.", sequence, claimable);
                    return CashShopBuyResult.PackageNotFound;
                }

                products.Add(product);
            }

            if (CashShopFeaturePlugIn.GetSettings(player.GameContext) is { MaximumStorageItems: > 0 } settings)
            {
                var storedItems = await player.PersistenceContext.GetCashShopStorageItemsAsync(account.GetId()).ConfigureAwait(false);
                if (storedItems.Count + products.Count > settings.MaximumStorageItems)
                {
                    return CashShopBuyResult.StorageFull;
                }
            }

            if (attribute is null)
            {
                attribute = player.PersistenceContext.CreateNew<StatAttribute>(definition, 0f);
                character.Attributes.Add(attribute);
            }

            var storageItems = new List<CashShopStorageItem>(products.Count);
            foreach (var product in products)
            {
                var storageItem = player.PersistenceContext.CreateNew<CashShopStorageItem>();
                storageItem.AccountId = account.GetId();
                storageItem.ProductSequence = product.ProductSequence;
                storageItem.PriceSequence = product.PriceSequence;
                storageItem.AddedAt = DateTime.UtcNow;
                storageItems.Add(storageItem);
            }

            attribute.Value = claimed | (1 << allOccurrences.IndexOf(claimable));
            var previousVipUntil = account.VipUntil;
            if (milestone.VipDays > 0)
            {
                // The days are added to a running VIP.
                var now = DateTime.UtcNow;
                account.VipUntil = (account.VipUntil > now ? account.VipUntil.Value : now).AddDays(milestone.VipDays);
            }

            bool saved;
            try
            {
                saved = await player.SaveProgressAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                player.Logger.LogError(ex, "Couldn't save the reset reward claim of player {player}.", player);
                saved = false;
            }

            if (!saved)
            {
                attribute.Value = claimed;
                account.VipUntil = previousVipUntil;
                foreach (var storageItem in storageItems)
                {
                    await player.PersistenceContext.DeleteAsync(storageItem).ConfigureAwait(false);
                }

                return CashShopBuyResult.SaveFailed;
            }

            player.Logger.LogInformation("Player {player} claimed the reward of {resets} resets: products {products}, {vipDays} VIP days.", player, claimable, string.Join(", ", products.Select(p => p.ProductSequence)), milestone.VipDays);
            message = milestone.VipDays > 0
                ? $"¡Canjeaste el premio de {claimable} resets! Tenés {milestone.VipDays} días más de VIP; los ítems están en el depósito de la tienda."
                : $"¡Canjeaste el premio de {claimable} resets! Retiralo del depósito de la tienda.";
            return CashShopBuyResult.Success;
        }).ConfigureAwait(false);

        if (result == CashShopBuyResult.Success && milestone.VipDays > 0)
        {
            VipPlugIn.UpdateBonuses(player);
        }

        if (message is not null)
        {
            await player.ShowBlueMessageAsync(message).ConfigureAwait(false);
        }

        return result;
    }
}
