// <copyright file="StackableJewelsPlugInGkz.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update makes the jewels stackable up to 50 pieces in one inventory slot.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("8C2E4A61-3D7B-4F95-A1C8-7E0B9D2F5A47")]
public class StackableJewelsPlugInGkz : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: stackable jewels";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Jewels (Bless, Soul, Life, Chaos, Creation, ...) stack up to 50 pieces in one inventory slot.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonThreeGkz.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 13, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        GkzStackableJewels.Apply(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
