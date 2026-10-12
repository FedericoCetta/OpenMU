// <copyright file="AddLorenciaBattleCenterPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update replaces the fountain of Lorencia by the PvP ring of MU GKZ.
/// Version 1 turned the whole plaza into a battle area; version 2 limits it to the ring.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5F23FA05-8AC3-4A55-80AC-927B57C5FDCF")]
public class AddLorenciaBattleCenterPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: Lorencia PvP ring";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Replaces the fountain of Lorencia by a PvP ring: inside of the ring there is no safezone. The spawn point of Lorencia is limited to the north part of the plaza. The terrain of Lorencia is reset to its original before. Requires the matching client (EncTerrain1.att/.obj and the ring model).";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        LorenciaBattleCenter.Apply(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
