// <copyright file="TestAccountsInitialization.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz.TestAccounts;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.TestAccounts;

/// <summary>
/// Creates the test accounts of MU GKZ. These are the Season 6 test accounts, without the ones
/// for removed content (socket items, Summoner, Rage Fighter) and without master level points.
/// </summary>
internal class TestAccountsInitialization : InitializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAccountsInitialization"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public TestAccountsInitialization(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        for (int i = 0; i < 10; i++)
        {
            var level = (i * 10) + 1;
            new LowLevel(this.Context, this.GameConfiguration, "test" + i, level).Initialize();
        }

        new Level300(this.Context, this.GameConfiguration).Initialize();
        new Level400WithoutMasterPoints(this.Context, this.GameConfiguration).Initialize();
        new Ancient(this.Context, this.GameConfiguration).Initialize();
        new Quest150(this.Context, this.GameConfiguration).Initialize();
        new Quest220(this.Context, this.GameConfiguration).Initialize();
        new Quest400(this.Context, this.GameConfiguration).Initialize();
        new GameMasterWithoutMasterPoints(this.Context, this.GameConfiguration).Initialize();
    }

    /// <summary>
    /// The level 400 test account, without master level points.
    /// </summary>
    private sealed class Level400WithoutMasterPoints : Level400
    {
        public Level400WithoutMasterPoints(IContext context, GameConfiguration gameConfiguration)
            : base(context, gameConfiguration)
        {
        }

        /// <inheritdoc />
        protected override Account CreateAccount()
        {
            var account = base.CreateAccount();
            account.Characters.ForEach(c => c.MasterLevelUpPoints = 0);
            return account;
        }
    }

    /// <summary>
    /// The game master test account, without master level points.
    /// </summary>
    private sealed class GameMasterWithoutMasterPoints : GameMaster
    {
        public GameMasterWithoutMasterPoints(IContext context, GameConfiguration gameConfiguration)
            : base(context, gameConfiguration)
        {
        }

        /// <inheritdoc />
        protected override Account CreateAccount()
        {
            var account = base.CreateAccount();
            account.Characters.ForEach(c => c.MasterLevelUpPoints = 0);
            return account;
        }
    }
}
