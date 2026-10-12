// <copyright file="AddLorenciaBattleCenterPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update turns the plaza around the fountain of Lorencia into the battle center of MU GKZ.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5F23FA05-8AC3-4A55-80AC-927B57C5FDCF")]
public class AddLorenciaBattleCenterPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: Lorencia battle center";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Turns the plaza around the fountain of Lorencia into a battle area without safezone, removes the fountain and moves the spawn point of Lorencia to the street west of the plaza. Requires the matching client terrain (EncTerrain1.att).";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        LorenciaBattleCenter.Apply(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
