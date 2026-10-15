// <copyright file="GkzResetRewardsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Resets;

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

    private IReadOnlyList<int> Packages(int resets) => GkzResetRewardsPlugIn.GetMilestonePackages(this._configuration, resets, DarkKnight);
}
