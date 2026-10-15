// <copyright file="GkzCashShop.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using System.Text.Json;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// The item shop of MU GKZ (X key), from <c>GkzCashShop.json</c>. The client script is built from the same file
/// by <c>tools/gkz_shop_script.py</c>, so the sequence numbers, names and prices always match.
/// Everything is paid with the GKZ Coins (WCoinC), and nothing in it gives power: talismans, event invitations
/// and transformation rings.
/// </summary>
internal static class GkzCashShop
{
    private const string ResourceName = "MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz.GkzCashShop.json";

    /// <summary>
    /// Gets the catalog.
    /// </summary>
    internal static Catalog Load()
    {
        using var stream = typeof(GkzCashShop).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Resource {ResourceName} not found.");
        return JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The shop catalog is empty.");
    }

    /// <summary>
    /// Creates the shop, or brings an existing one up to date with the catalog: versions, and the packages by
    /// their sequence number. Packages which aren't in the catalog any more are no longer for sale.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        var catalog = Load();

        // The claims of the reset rewards (packages of the Reset category) are kept in this attribute.
        if (gameConfiguration.Attributes.All(a => a.Id != GkzResetRewardClaims.ClaimedAttribute.Id))
        {
            var claimed = GkzResetRewardClaims.ClaimedAttribute;
            gameConfiguration.Attributes.Add(context.CreateNew<AttributeDefinition>(claimed.Id, claimed.Designation, claimed.Description));
        }

        var configuration = gameConfiguration.CashShopConfiguration;
        if (configuration is null)
        {
            configuration = context.CreateNew<CashShopConfiguration>();
            gameConfiguration.CashShopConfiguration = configuration;
        }

        configuration.ScriptSaleZone = catalog.ScriptVersion.Zone;
        configuration.ScriptYear = catalog.ScriptVersion.Year;
        configuration.ScriptYearId = catalog.ScriptVersion.YearId;
        configuration.BannerSaleZone = catalog.BannerVersion.Zone;
        configuration.BannerYear = catalog.BannerVersion.Year;
        configuration.BannerYearId = catalog.BannerVersion.YearId;

        var sequences = catalog.Packages.Select(p => p.Seq).ToHashSet();
        foreach (var obsolete in configuration.Packages.Where(p => !sequences.Contains(p.PackageSequence)))
        {
            obsolete.IsForSale = false;
        }

        foreach (var entry in catalog.Packages)
        {
            var package = configuration.Packages.FirstOrDefault(p => p.PackageSequence == entry.Seq);
            if (package is null)
            {
                package = context.CreateNew<CashShopPackage>();
                package.PackageSequence = entry.Seq;
                configuration.Packages.Add(package);
            }

            var forSale = entry.ForSale ?? true;
            package.Name = entry.Name;
            package.Price = entry.Price;
            package.CoinType = entry.Coin == "reset" ? CashShopCoinType.GoblinPoints : CashShopCoinType.WCoinC;
            package.IsGiftable = forSale && !GkzResetRewardClaims.IsClaimPackage(entry.Seq);
            package.IsBundle = false;

            // One product per package, with the sequence number of the package as product and price sequence.
            var product = package.Products.FirstOrDefault();
            if (product is null)
            {
                product = context.CreateNew<CashShopProduct>();
                package.Products.Add(product);
            }

            product.ProductSequence = entry.Seq;
            product.PriceSequence = entry.Seq;
            product.Price = entry.Price;
            product.ItemDefinition = gameConfiguration.Items.FirstOrDefault(i => i.Group == entry.Group && i.Number == entry.Number);
            product.ItemLevel = entry.Level;
            product.Quantity = entry.Quantity ?? 1;
            product.Duration = TimeSpan.Zero;

            // The rewards aren't for sale; they're what the claims of the Reset category give (see GkzResetRewardClaims).
            package.IsForSale = forSale && product.CanBeDelivered();
        }
    }

    /// <summary>
    /// The catalog of <c>GkzCashShop.json</c>.
    /// </summary>
    internal sealed record Catalog(Version ScriptVersion, Version BannerVersion, IReadOnlyList<Package> Packages);

    /// <summary>
    /// A script or banner version.
    /// </summary>
    internal sealed record Version(short Zone, short Year, short YearId);

    /// <summary>
    /// A package of the catalog: one item, or a stack of it (<paramref name="Quantity"/>).
    /// </summary>
    /// <param name="Seq">The sequence number of the package, product and price.</param>
    /// <param name="Category">The category in the client.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Price">The price.</param>
    /// <param name="Group">The group of the item.</param>
    /// <param name="Number">The number of the item.</param>
    /// <param name="Level">The level of the item.</param>
    /// <param name="Description">The description.</param>
    /// <param name="Coin">"reset" for GKZ Reset Coins (the Goblin Points), otherwise GKZ Coins.</param>
    /// <param name="Quantity">The quantity of the item; stackable items come in stacks.</param>
    /// <param name="ForSale"><c>false</c> for packages which are only given away, e.g. reset rewards.</param>
    internal sealed record Package(int Seq, int Category, string Name, int Price, byte Group, short Number, byte Level, string Description, string? Coin = null, int? Quantity = null, bool? ForSale = null);
}
