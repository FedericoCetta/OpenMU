// <copyright file="GkzResetRewardsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.Persistence;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the reset rewards of MU GKZ (<see cref="GkzResetRewardsPlugIn"/>).
/// </summary>
[TestFixture]
public class GkzResetRewardsTest
{
    private const int DarkKnight = 1;

    private readonly GkzResetRewardsConfiguration _configuration = new();

    /// <summary>
    /// Tests the milestones which were decided, including the wings of the class.
    /// </summary>
    [Test]
    public void MilestonesGiveTheDecidedPackages()
    {
        Assert.That(this.Packages(10), Is.EqualTo(new[] { GkzResetRewardPackages.Soul10, GkzResetRewardPackages.Bless10 }));
        Assert.That(this.Packages(25), Is.EqualTo(new[] { GkzResetRewardPackages.SecondWings + DarkKnight }));
        Assert.That(this.Packages(100), Is.EqualTo(new[] { GkzResetRewardPackages.ThirdWings + DarkKnight }));
        Assert.That(this.Packages(200), Has.Count.EqualTo(3));
        Assert.That(this.Packages(11), Is.Empty);
    }

    /// <summary>
    /// Tests that the milestones of 125 to 200 repeat every 100 resets until 500, and that there's nothing after it.
    /// </summary>
    [Test]
    public void MilestonesRepeatUntil500()
    {
        Assert.That(this.Packages(225), Is.EqualTo(this.Packages(125)));
        Assert.That(this.Packages(300), Is.EqualTo(this.Packages(200)));
        Assert.That(this.Packages(475), Is.EqualTo(this.Packages(175)));
        Assert.That(this.Packages(500), Is.EqualTo(this.Packages(200)));
        Assert.That(this.Packages(525), Is.Empty);
    }

    /// <summary>
    /// Tests the class index of the class specific rewards for the first, second and third classes.
    /// </summary>
    /// <param name="classNumber">The number of the character class.</param>
    /// <param name="expectedIndex">The expected index.</param>
    [TestCase((byte)0, 0)]
    [TestCase((byte)3, 0)]
    [TestCase((byte)4, 1)]
    [TestCase((byte)7, 1)]
    [TestCase((byte)8, 2)]
    [TestCase((byte)11, 2)]
    [TestCase((byte)12, 3)]
    [TestCase((byte)13, 3)]
    [TestCase((byte)16, 4)]
    [TestCase((byte)17, 4)]
    public void ClassIndexOfTheWings(byte classNumber, int expectedIndex)
    {
        Assert.That(GkzResetRewardsPlugIn.GetClassIndex(classNumber), Is.EqualTo(expectedIndex));
    }

    /// <summary>
    /// Tests that a reset gives the Reset Coins, with the bonus every 10 resets.
    /// </summary>
    [Test]
    public async Task ResetGivesResetCoinsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.GoblinPoints = 0;
        var plugIn = new GkzResetRewardsPlugIn { Configuration = this._configuration };

        await plugIn.PlayerResetAsync(player, 9).ConfigureAwait(false);
        Assert.That(player.Account.GoblinPoints, Is.EqualTo(10));

        await plugIn.PlayerResetAsync(player, 10).ConfigureAwait(false);
        Assert.That(player.Account.GoblinPoints, Is.EqualTo(10 + 10 + 50));
    }

    /// <summary>
    /// Tests which occurrence of a milestone can be claimed: the first one which was reached and not claimed yet.
    /// </summary>
    [Test]
    public void ClaimableOccurrence()
    {
        var tenResets = this._configuration.Milestones[0];
        var hundredTwentyFive = this._configuration.Milestones.Single(m => m.Resets == 125);
        var all = GkzResetRewardClaims.GetAllOccurrences(this._configuration);

        Assert.That(all, Has.Count.EqualTo(21), "10, 25, 50, 75, 100, then every 25 until 500");
        Assert.That(GkzResetRewardClaims.GetClaimable(this._configuration, tenResets, 9, 0), Is.Zero, "not reached");
        Assert.That(GkzResetRewardClaims.GetClaimable(this._configuration, tenResets, 300, 0), Is.EqualTo(10), "reached long ago, still there");
        Assert.That(GkzResetRewardClaims.GetClaimable(this._configuration, tenResets, 300, 1), Is.Zero, "claimed");
        Assert.That(GkzResetRewardClaims.GetClaimable(this._configuration, hundredTwentyFive, 300, 0), Is.EqualTo(125));
        var claimed125 = 1 << all.IndexOf(125);
        Assert.That(GkzResetRewardClaims.GetClaimable(this._configuration, hundredTwentyFive, 300, claimed125), Is.EqualTo(225));
        Assert.That(GkzResetRewardClaims.GetClaimable(this._configuration, hundredTwentyFive, 224, claimed125), Is.Zero);
    }

    /// <summary>
    /// Tests the claim in the shop: the items of the milestone go into the storage, once; before the milestone,
    /// nothing is given.
    /// </summary>
    [Test]
    public async Task ClaimInTheShopAsync()
    {
        var player = await this.CreateClaimPlayerAsync().ConfigureAwait(false);
        var shop = player.GameContext.Configuration.CashShopConfiguration!;
        var accountId = player.Account!.GetId();
        player.Attributes![Stats.Resets] = 9;

        Assert.That(await GkzResetRewardClaims.ClaimAsync(player, shop, GkzResetRewardPackages.FirstClaim).ConfigureAwait(false), Is.EqualTo(CashShopBuyResult.RefusedByPlugIn));
        Assert.That(await player.PersistenceContext.GetCashShopStorageItemsAsync(accountId).ConfigureAwait(false), Is.Empty);

        player.Attributes[Stats.Resets] = 12;
        Assert.That(await GkzResetRewardClaims.ClaimAsync(player, shop, GkzResetRewardPackages.FirstClaim).ConfigureAwait(false), Is.EqualTo(CashShopBuyResult.Success));
        var stored = await player.PersistenceContext.GetCashShopStorageItemsAsync(accountId).ConfigureAwait(false);
        Assert.That(stored.Select(s => s.ProductSequence), Is.EquivalentTo(new[] { GkzResetRewardPackages.Soul10, GkzResetRewardPackages.Bless10 }));
        Assert.That(stored.All(s => !s.IsGift), Is.True);

        Assert.That(await GkzResetRewardClaims.ClaimAsync(player, shop, GkzResetRewardPackages.FirstClaim).ConfigureAwait(false), Is.EqualTo(CashShopBuyResult.RefusedByPlugIn), "only once");
        Assert.That(await player.PersistenceContext.GetCashShopStorageItemsAsync(accountId).ConfigureAwait(false), Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests that the wings of the claim are the ones of the class of the character.
    /// </summary>
    [Test]
    public async Task ClaimGivesTheWingsOfTheClassAsync()
    {
        var player = await this.CreateClaimPlayerAsync().ConfigureAwait(false);
        var shop = player.GameContext.Configuration.CashShopConfiguration!;
        player.Attributes![Stats.Resets] = 25;

        Assert.That(await GkzResetRewardClaims.ClaimAsync(player, shop, GkzResetRewardPackages.FirstClaim + 1).ConfigureAwait(false), Is.EqualTo(CashShopBuyResult.Success));
        var stored = await player.PersistenceContext.GetCashShopStorageItemsAsync(player.Account!.GetId()).ConfigureAwait(false);
        var classIndex = GkzResetRewardsPlugIn.GetClassIndex(player.SelectedCharacter!.CharacterClass!.Number);
        Assert.That(stored.Single().ProductSequence, Is.EqualTo(GkzResetRewardPackages.SecondWings + classIndex));
    }

    private async ValueTask<Player> CreateClaimPlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.FeaturePlugIns.AddPlugIn(new CashShopFeaturePlugIn { Configuration = new CashShopSettings() }, true);
        player.GameContext.FeaturePlugIns.AddPlugIn(new GkzResetRewardsPlugIn { Configuration = this._configuration }, true);
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Attributes).Returns(new List<AttributeSystem.AttributeDefinition> { GkzResetRewardClaims.ClaimedAttribute });
        var shop = new BasicModel.CashShopConfiguration();
        foreach (var sequence in new[] { 101, 102, 103, 104, 105, 106, 107, 108, 111, 112, 113, 114, 115, 121, 122, 123, 124, 125 })
        {
            var package = new BasicModel.CashShopPackage { PackageSequence = sequence, CoinType = CashShopCoinType.GoblinPoints };
            package.Products.Add(new BasicModel.CashShopProduct
            {
                ProductSequence = sequence,
                PriceSequence = sequence,
                Quantity = 10,
                ItemDefinition = new ItemDefinition { Width = 1, Height = 1, Durability = 1 },
            });
            shop.Packages.Add(package);
        }

        player.GameContext.Configuration.CashShopConfiguration = shop;
        return player;
    }

    private IReadOnlyList<int> Packages(int resets) => GkzResetRewardsPlugIn.GetMilestonePackages(this._configuration, resets, DarkKnight);
}
