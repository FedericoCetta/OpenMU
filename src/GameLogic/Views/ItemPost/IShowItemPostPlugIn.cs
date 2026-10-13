// <copyright file="IShowItemPostPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.ItemPost;

/// <summary>
/// MU GKZ: shows an item which a player posted to the chat.
/// </summary>
public interface IShowItemPostPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the posted item in the chat.
    /// </summary>
    /// <param name="sender">The name of the character which posted it.</param>
    /// <param name="item">The item.</param>
    ValueTask ShowItemPostAsync(string sender, Item item);
}
