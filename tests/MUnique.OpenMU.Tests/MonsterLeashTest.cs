// <copyright file="MonsterLeashTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Tests for the <see cref="MonsterLeash"/>, which keeps the monsters near their spot.
/// </summary>
[TestFixture]
public class MonsterLeashTest
{
    private readonly MonsterSpawnArea _area = new() { X1 = 100, Y1 = 100, X2 = 104, Y2 = 104 };

    /// <summary>
    /// Tests that a step inside the spot plus the margin isn't changed.
    /// </summary>
    [Test]
    public void StepNearTheSpotIsKept()
    {
        Assert.That(MonsterLeash.GetRandomStepTarget(new Point(102, 102), 3, -3, this._area, 3), Is.EqualTo(new Point(105, 99)));
    }

    /// <summary>
    /// Tests that a step can't leave the spot by more than the margin.
    /// </summary>
    [Test]
    public void StepDoesNotLeaveTheSpot()
    {
        Assert.That(MonsterLeash.GetRandomStepTarget(new Point(106, 106), 3, 3, this._area, 3), Is.EqualTo(new Point(107, 107)));
    }

    /// <summary>
    /// Tests that a monster far away from its spot (after a chase) walks back towards it.
    /// </summary>
    [Test]
    public void MonsterFarAwayWalksBack()
    {
        Assert.That(MonsterLeash.GetRandomStepTarget(new Point(130, 80), 2, 0, this._area, 3), Is.EqualTo(new Point(107, 97)));
    }

    /// <summary>
    /// Tests that a monster without a spawn area walks freely, but stays on the map.
    /// </summary>
    [Test]
    public void WithoutSpawnAreaOnlyTheMapLimits()
    {
        Assert.That(MonsterLeash.GetRandomStepTarget(new Point(254, 1), 3, -3, null, 3), Is.EqualTo(new Point(255, 0)));
    }
}
