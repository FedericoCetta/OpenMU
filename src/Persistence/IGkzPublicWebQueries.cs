// <copyright file="IGkzPublicWebQueries.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using System.Threading;

/// <summary>
/// The orders of the character rankings of the public website.
/// </summary>
public enum GkzCharacterRankingOrder
{
    /// <summary>By resets, then level.</summary>
    Resets,

    /// <summary>By level, then resets.</summary>
    Level,

    /// <summary>By player kills.</summary>
    Kills,
}

/// <summary>
/// A character of a ranking of the public website. Only public data: no account, no ids.
/// </summary>
public sealed class GkzRankedCharacter
{
    /// <summary>Gets or sets the name of the character.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of the character class.</summary>
    public short ClassNumber { get; set; }

    /// <summary>Gets or sets the level.</summary>
    public int Level { get; set; }

    /// <summary>Gets or sets the resets.</summary>
    public int Resets { get; set; }

    /// <summary>Gets or sets the player kills.</summary>
    public int Kills { get; set; }

    /// <summary>Gets or sets the name of the guild, if any.</summary>
    public string? Guild { get; set; }
}

/// <summary>
/// A guild of a ranking of the public website.
/// </summary>
public sealed class GkzRankedGuild
{
    /// <summary>Gets or sets the name of the guild.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the guild master.</summary>
    public string? Master { get; set; }

    /// <summary>Gets or sets the number of members.</summary>
    public int Members { get; set; }

    /// <summary>Gets or sets the score.</summary>
    public int Score { get; set; }

    /// <summary>Gets or sets the guild mark (32 bytes, 8x8 pixels of 4 bits).</summary>
    public byte[]? Logo { get; set; }
}

/// <summary>
/// The read-only queries of the public website of MU GKZ. They take no text from the visitors:
/// the SQL is fixed, so nothing can be injected.
/// </summary>
public interface IGkzPublicWebQueries
{
    /// <summary>
    /// Gets the best characters of real players (no bots, game masters or banned accounts).
    /// </summary>
    /// <param name="order">The order of the ranking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The best 50 characters.</returns>
    Task<IReadOnlyList<GkzRankedCharacter>> GetCharacterRankingAsync(GkzCharacterRankingOrder order, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the best guilds by score.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The best 50 guilds.</returns>
    Task<IReadOnlyList<GkzRankedGuild>> GetGuildRankingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the guild which owns the castle.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The owner guild; <c>null</c> when the castle has no owner.</returns>
    Task<GkzRankedGuild?> GetCastleOwnerAsync(CancellationToken cancellationToken);
}
