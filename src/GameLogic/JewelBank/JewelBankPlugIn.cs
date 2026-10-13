// <copyright file="JewelBankPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The jewel bank of MU GKZ: an account keeps numbers of jewels, boxes and talismans, outside of the inventory and vaults.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: jewel bank", Description = "A bank for jewels, boxes and talismans of the account, which the client opens from the inventory.")]
[Guid("A7D3F1B2-6C4E-4B89-8F1A-2D5E7C9B3A64")]
public class JewelBankPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<JewelBankConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public JewelBankConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the configuration of the bank, if the plugin is active.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The configuration; <c>null</c>, if the bank isn't active.</returns>
    public static JewelBankConfiguration? GetConfiguration(IGameContext gameContext)
    {
        return gameContext.FeaturePlugIns.GetPlugIn<JewelBankPlugIn>() is { } plugIn
            ? plugIn.Configuration ?? new JewelBankConfiguration()
            : null;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new JewelBankConfiguration();
}
