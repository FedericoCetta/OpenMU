// <copyright file="IShowJewelBankPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.JewelBank;

using MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>
/// Interface of a view whose implementation shows the jewel bank of the account.
/// </summary>
public interface IShowJewelBankPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the items of the bank and how many of each it holds.
    /// </summary>
    /// <param name="items">The items, in the order of the configuration, with the number in the bank.</param>
    /// <param name="autoSave">Whether picked up items go straight into the bank.</param>
    ValueTask ShowJewelBankAsync(IReadOnlyList<(JewelBankEntry Entry, int Count)> items, bool autoSave);
}
