// <copyright file="GameMapsInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

/// <summary>
/// The maps initializer of MU GKZ, which only knows the maps of Season 3.
/// </summary>
internal class GameMapsInitializer : VersionSeasonSix.GameMapsInitializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameMapsInitializer"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public GameMapsInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<Type> MapInitializerTypes =>
        base.MapInitializerTypes.Where(type => !SeasonThreeScope.RemovedMapInitializerTypes.Contains(type));
}
