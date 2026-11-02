// <copyright file="IGkzPlayerPanelQueries.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using System.Threading;

/// <summary>
/// The account of a player, as the player panel of the website shows it.
/// </summary>
public class GkzPanelAccount
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the login name.</summary>
    public string LoginName { get; set; } = string.Empty;

    /// <summary>Gets or sets the password hash (BCrypt).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the e-mail address.</summary>
    public string EMail { get; set; } = string.Empty;

    /// <summary>Gets or sets the state (<c>DataModel.Entities.AccountState</c>).</summary>
    public int State { get; set; }

    /// <summary>Gets or sets the registration date.</summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>Gets or sets the end of the VIP.</summary>
    public DateTime? VipUntil { get; set; }

    /// <summary>Gets or sets the GKZ Coins (WCoin C).</summary>
    public int Coins { get; set; }

    /// <summary>Gets or sets the GKZ Coins which were granted, but not applied yet by the game server.</summary>
    public int PendingCoins { get; set; }

    /// <summary>Gets or sets a value indicating whether it's a bot or template account.</summary>
    public bool IsBotOrTemplate { get; set; }
}

/// <summary>
/// A character of an account, as the player panel of the website shows it.
/// </summary>
public class GkzPanelCharacter
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of the character class.</summary>
    public short ClassNumber { get; set; }

    /// <summary>Gets or sets the level.</summary>
    public int Level { get; set; }

    /// <summary>Gets or sets the resets.</summary>
    public int Resets { get; set; }

    /// <summary>Gets or sets the name of the map.</summary>
    public string? Map { get; set; }

    /// <summary>Gets or sets the kills of players.</summary>
    public int Kills { get; set; }

    /// <summary>Gets or sets the guild.</summary>
    public string? Guild { get; set; }
}

/// <summary>
/// A purchase of GKZ Coins of an account (a grant of a payment provider), as the player panel shows it.
/// </summary>
public class GkzPanelPurchase
{
    /// <summary>Gets or sets the description, e.g. the provider and the pack.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets the coins; negative for a refund.</summary>
    public int Amount { get; set; }

    /// <summary>Gets or sets the timestamp of the grant.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the game server already added the coins to the balance.</summary>
    public bool IsApplied { get; set; }
}

/// <summary>
/// The queries of the player panel of the MU GKZ website. All of them are parameterized,
/// so the text of the visitors can't change the SQL.
/// </summary>
public interface IGkzPlayerPanelQueries
{
    /// <summary>
    /// Gets an account by its login name.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account; <c>null</c> if it doesn't exist.</returns>
    Task<GkzPanelAccount?> GetAccountAsync(string loginName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an account by its id.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account; <c>null</c> if it doesn't exist.</returns>
    Task<GkzPanelAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the characters of an account.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The characters, by slot.</returns>
    Task<IReadOnlyList<GkzPanelCharacter>> GetCharactersAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the password hash of an account.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="passwordHash">The new password hash (BCrypt).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c>, if the account was changed.</returns>
    Task<bool> SetPasswordHashAsync(Guid accountId, string passwordHash, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the e-mail address of an account.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="email">The new e-mail address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c>, if the account was changed.</returns>
    Task<bool> SetEMailAsync(Guid accountId, string email, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a character of an account to the safe zone of Lorencia.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="characterName">The name of the character.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c>, if the character was moved; <c>false</c>, if the account has no such character.</returns>
    Task<bool> MoveToLorenciaAsync(Guid accountId, string characterName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the latest purchases of GKZ Coins of an account: the grants of the payment providers,
    /// whose references start with <c>mp:</c> (Mercado Pago) or <c>pp:</c> (PayPal).
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest 20 purchases, the newest first.</returns>
    Task<IReadOnlyList<GkzPanelPurchase>> GetPurchasesAsync(Guid accountId, CancellationToken cancellationToken);
}
