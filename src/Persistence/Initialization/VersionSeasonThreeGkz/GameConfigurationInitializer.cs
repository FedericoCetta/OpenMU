// <copyright file="GameConfigurationInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

/// <summary>
/// Initializes the game configuration of MU GKZ: the Season 6 data, trimmed down to Season 3.
/// </summary>
internal class GameConfigurationInitializer : VersionSeasonSix.GameConfigurationInitializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigurationInitializer"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public GameConfigurationInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        // There is no master level in Season 3. The third classes still are master classes,
        // because the client and the mini games (e.g. Blood Castle 8) rely on that.
        this.GameConfiguration.MaximumMasterLevel = 0;

        new SeasonThreeContentPruner(this.Context, this.GameConfiguration).Initialize();
        LorenciaBattleCenter.Apply(this.GameConfiguration);
    }
}
