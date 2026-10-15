// <copyright file="GkzResetMilestone.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// A milestone of the reset rewards.
/// </summary>
public class GkzResetMilestone
{
    /// <summary>
    /// Gets or sets the number of resets.
    /// </summary>
    public int Resets { get; set; }

    /// <summary>
    /// Gets or sets the sequence numbers of the shop packages which are given.
    /// </summary>
    public List<int> Packages { get; set; } = [];

    /// <summary>
    /// Gets or sets the wings of the class which are given: 2 for the second, 3 for the third wings; 0 for none.
    /// </summary>
    public int ClassWings { get; set; }
}
