// <copyright file="DataInitialization.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Initialization of the MU GKZ data: the Season 6 data trimmed down to the content of Season 3.
/// The game client stays the Season 6 Episode 3 client.
/// </summary>
[Guid("9F542D19-D462-4F88-B81A-5F89FB339B0B")]
[PlugIn]
[Display(Name = nameof(PlugInResources.DataInitializationSeasonThreeGkz_Name), Description = nameof(PlugInResources.DataInitializationSeasonThreeGkz_Description), ResourceType = typeof(PlugInResources))]
public class DataInitialization : VersionSeasonSix.DataInitialization
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataInitialization"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public DataInitialization(IPersistenceContextProvider persistenceContextProvider, ILoggerFactory loggerFactory)
        : base(persistenceContextProvider, loggerFactory)
    {
    }

    /// <summary>
    /// Gets the id of this data initialization. It's persisted in the database, so it must never change.
    /// </summary>
    public static new string Id => "season3-gkz";

    /// <inheritdoc />
    public override string Key => Id;

    /// <inheritdoc />
    public override string Caption => "MU GKZ - Season 3";

    /// <inheritdoc />
    protected override IInitializer TestAccountsInitializer => new TestAccounts.TestAccountsInitialization(this.Context, this.GameConfiguration);

    /// <inheritdoc />
    protected override IInitializer GameConfigurationInitializer => new GameConfigurationInitializer(this.Context, this.GameConfiguration);

    /// <inheritdoc />
    protected override IGameMapsInitializer GameMapsInitializer => new GameMapsInitializer(this.Context, this.GameConfiguration);

    /// <inheritdoc />
    protected override void OnPlugInConfigurationCreated(Type plugInType, PlugInConfiguration plugInConfiguration)
    {
        base.OnPlugInConfigurationCreated(plugInType, plugInConfiguration);
        if (SeasonThreeScope.DeactivatedPlugIns.Contains(plugInType))
        {
            plugInConfiguration.IsActive = false;
        }
    }
}
