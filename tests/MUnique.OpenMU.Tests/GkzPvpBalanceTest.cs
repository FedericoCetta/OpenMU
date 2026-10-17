// <copyright file="GkzPvpBalanceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PvP;

/// <summary>
/// Tests for the PvP balance of MU GKZ.
/// </summary>
[TestFixture]
public class GkzPvpBalanceTest
{
    /// <summary>
    /// Tests that the default configuration has every pair of classes once, at 100 %, and 70 % in general.
    /// </summary>
    [Test]
    public void DefaultHasAllPairsAndSeventyPercent()
    {
        var configuration = new GkzPvpBalanceConfiguration();

        Assert.That(configuration.ClassPairs, Has.Count.EqualTo(25));
        Assert.That(configuration.ClassPairs.Select(p => (p.Attacker, p.Victim)), Is.Unique);
        Assert.That(configuration.ClassPairs.All(p => p.Percent == 100), Is.True);
        Assert.That(configuration.GetFactor(GkzPvpClass.DarkKnight, GkzPvpClass.DarkWizard, GkzPvpPlace.Normal), Is.EqualTo(0.7).Within(0.0001));
        Assert.That(
            (configuration.MaxDamageReductionPercent, configuration.MaxSoulBarrierPercent, configuration.MaxGreaterDamageBonus, configuration.MaxGreaterDefenseBonus,
                configuration.MaxReflectPercent, configuration.MaxCriticalChancePercent, configuration.MaxExcellentChancePercent),
            Is.EqualTo((60, 60, 1000, 800, 70, 80, 70)));
    }

    /// <summary>
    /// Tests that the general, place and pair percentages are multiplied.
    /// </summary>
    [Test]
    public void FactorMultipliesGeneralPlaceAndPair()
    {
        var configuration = new GkzPvpBalanceConfiguration { GeneralPercent = 80, DuelPercent = 50 };
        configuration.ClassPairs.Single(p => p.Attacker == GkzPvpClass.DarkKnight && p.Victim == GkzPvpClass.Elf).Percent = 75;

        Assert.That(configuration.GetFactor(GkzPvpClass.DarkKnight, GkzPvpClass.Elf, GkzPvpPlace.Duel), Is.EqualTo(0.8 * 0.5 * 0.75).Within(0.0001));
        Assert.That(configuration.GetFactor(GkzPvpClass.Elf, GkzPvpClass.DarkKnight, GkzPvpPlace.Duel), Is.EqualTo(0.4).Within(0.0001));
        Assert.That(configuration.GetFactor(GkzPvpClass.DarkKnight, GkzPvpClass.Elf, GkzPvpPlace.Normal), Is.EqualTo(0.6).Within(0.0001));
    }

    /// <summary>
    /// Tests that a missing pair counts as 100 % and negative percentages as 0.
    /// </summary>
    [Test]
    public void MissingPairIsHundredAndNegativeIsZero()
    {
        var configuration = new GkzPvpBalanceConfiguration { GeneralPercent = 100, ClassPairs = [] };
        Assert.That(configuration.GetFactor(GkzPvpClass.DarkLord, GkzPvpClass.MagicGladiator, GkzPvpPlace.CastleSiege), Is.EqualTo(1.0).Within(0.0001));

        configuration.CastleSiegePercent = -10;
        Assert.That(configuration.GetFactor(GkzPvpClass.DarkLord, GkzPvpClass.MagicGladiator, GkzPvpPlace.CastleSiege), Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the character classes map to their base class.
    /// </summary>
    [Test]
    public void ClassesMapToTheirBaseClass()
    {
        Assert.That(new byte[] { 0, 2, 3 }.Select(GkzPvpBalancePlugIn.GetClass), Is.All.EqualTo(GkzPvpClass.DarkWizard));
        Assert.That(new byte[] { 4, 6, 7 }.Select(GkzPvpBalancePlugIn.GetClass), Is.All.EqualTo(GkzPvpClass.DarkKnight));
        Assert.That(new byte[] { 8, 10, 11 }.Select(GkzPvpBalancePlugIn.GetClass), Is.All.EqualTo(GkzPvpClass.Elf));
        Assert.That(new byte[] { 12, 13 }.Select(GkzPvpBalancePlugIn.GetClass), Is.All.EqualTo(GkzPvpClass.MagicGladiator));
        Assert.That(new byte[] { 16, 17 }.Select(GkzPvpBalancePlugIn.GetClass), Is.All.EqualTo(GkzPvpClass.DarkLord));
    }
}
