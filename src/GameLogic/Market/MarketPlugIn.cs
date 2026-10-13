// <copyright file="MarketPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// MU GKZ: the market, where players sell items to each other for zen or coins, even while they're offline.
/// The client opens it with the O key, in a safe zone.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: market", Description = "The market of the O key: players sell items for zen or GKZ Coins, even while they're offline.")]
[Guid("6B1E9F43-2C7A-4D85-A0E3-9F4B7C2D8E51")]
public class MarketPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<MarketConfiguration>, ISupportDefaultCustomConfiguration, IDisposable
{
    private MarketService? _service;

    /// <inheritdoc />
    public MarketConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the market of the game context, if the plugin is active.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The market; <c>null</c>, if it isn't active.</returns>
    public static MarketService? GetService(IGameContext gameContext)
    {
        return gameContext.FeaturePlugIns.GetPlugIn<MarketPlugIn>() is { } plugIn
            ? plugIn._service ??= new MarketService(gameContext, () => plugIn.Configuration ?? new MarketConfiguration())
            : null;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MarketConfiguration();

    /// <inheritdoc />
    public void Dispose()
    {
        this._service?.Dispose();
        this._service = null;
    }
}
