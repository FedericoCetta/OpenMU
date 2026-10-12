// <copyright file="VipPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Vip;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The VIP of MU GKZ: VIP accounts get more experience, item drops and zen, and more vaults.
/// </summary>
/// <remarks>
/// The account is VIP until <see cref="Account.VipUntil"/>. The bonuses are added to the attributes of the
/// character while it's in the game, and removed when the VIP ends, which is checked every few seconds.
/// </remarks>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C3B5E2A1-7D4F-4A86-9E21-5B0C8D7F4A93")]
public class VipPlugIn : IFeaturePlugIn, IPeriodicTaskPlugIn, IPlayerStateChangedPlugIn, ISupportCustomConfiguration<VipConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: VIP";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "VIP accounts, bought with coins: more experience, item drops and zen, and up to six vaults.";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The bonuses which were added, by the attribute system of the character.
    /// A new character selection creates a new attribute system, so it starts without bonuses.
    /// </summary>
    private static readonly ConditionalWeakTable<IAttributeSystem, VipBonus> Bonuses = new();

    private DateTime _nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public VipConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the configuration of the VIP, if the plugin is active.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The configuration; <c>null</c>, if the VIP isn't active.</returns>
    public static VipConfiguration? GetConfiguration(IGameContext gameContext)
    {
        return gameContext.FeaturePlugIns.GetPlugIn<VipPlugIn>() is { } plugIn
            ? plugIn.Configuration ?? new VipConfiguration()
            : null;
    }

    /// <summary>
    /// Adds or removes the VIP bonuses of the player, so that they match its VIP state.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the bonuses were removed because the VIP ended.</returns>
    public static bool UpdateBonuses(Player player)
    {
        if (player.Attributes is not { } attributes)
        {
            return false;
        }

        var configuration = GetConfiguration(player.GameContext);
        var shouldHave = configuration is not null
                         && player.PlayerState.CurrentState == PlayerState.EnteredWorld
                         && player.Account?.IsVip() is true;
        var has = Bonuses.TryGetValue(attributes, out var bonus);
        if (shouldHave && !has)
        {
            bonus = new VipBonus(configuration!);
            bonus.AddTo(attributes);
            Bonuses.AddOrUpdate(attributes, bonus);
        }
        else if (!shouldHave && has)
        {
            bonus!.RemoveFrom(attributes);
            Bonuses.Remove(attributes);
            return player.PlayerState.CurrentState == PlayerState.EnteredWorld;
        }

        return false;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new VipConfiguration();

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var now = DateTime.UtcNow;
        if (now < this._nextRunUtc)
        {
            return;
        }

        this._nextRunUtc = now + CheckInterval;
        foreach (var player in await gameContext.GetPlayersAsync().ConfigureAwait(false))
        {
            if (UpdateBonuses(player))
            {
                await player.ShowBlueMessageAsync("Tu VIP terminó. Lo podés renovar en el Menú VIP de la ventana de personaje.").ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public void ForceStart() => this._nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        UpdateBonuses(player);
        if (previousState == PlayerState.CharacterSelection
            && currentState == PlayerState.EnteredWorld
            && player.Account is { } account
            && account.IsVip())
        {
            await player.ShowBlueMessageAsync($"VIP activo hasta el {account.VipUntil!.Value.ToLocalTime():dd/MM/yyyy HH:mm}.").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The bonuses which a VIP gets, as elements of the attribute system.
    /// </summary>
    private sealed class VipBonus
    {
        private readonly SimpleElement _experience;
        private readonly SimpleElement _money;
        private readonly SimpleElement _itemDropChance;
        private readonly SimpleElement _isVip = new(1, AggregateType.AddRaw);

        public VipBonus(VipConfiguration configuration)
        {
            this._experience = new SimpleElement(1 + configuration.ExperienceBonus, AggregateType.Multiplicate);
            this._money = new SimpleElement(1 + configuration.MoneyBonus, AggregateType.Multiplicate);
            this._itemDropChance = new SimpleElement(configuration.ItemDropChanceBonus, AggregateType.AddRaw);
        }

        public void AddTo(IAttributeSystem attributes)
        {
            attributes.AddElement(this._experience, Stats.ExperienceRate);
            attributes.AddElement(this._experience, Stats.MasterExperienceRate);
            attributes.AddElement(this._money, Stats.MoneyAmountRate);
            attributes.AddElement(this._itemDropChance, Stats.ItemDropChanceBonus);
            attributes.AddElement(this._isVip, Stats.IsVip);
        }

        public void RemoveFrom(IAttributeSystem attributes)
        {
            attributes.RemoveElement(this._experience, Stats.ExperienceRate);
            attributes.RemoveElement(this._experience, Stats.MasterExperienceRate);
            attributes.RemoveElement(this._money, Stats.MoneyAmountRate);
            attributes.RemoveElement(this._itemDropChance, Stats.ItemDropChanceBonus);
            attributes.RemoveElement(this._isVip, Stats.IsVip);
        }
    }
}
