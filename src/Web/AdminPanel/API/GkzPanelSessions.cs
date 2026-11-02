// <copyright file="GkzPanelSessions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Collections.Concurrent;
using System.Security.Cryptography;

/// <summary>
/// The sessions of the player panel of the MU GKZ website, the failed logins and the links to
/// recover a password. Everything is kept in memory: a restart of the server only logs the players
/// out of the panel and invalidates the recovery links that weren't used yet.
/// </summary>
/// <remarks>
/// The tokens are random (256 bits) and only their SHA-256 hashes are kept, so not even a memory
/// dump of the server gives a usable token.
/// </remarks>
public class GkzPanelSessions
{
    /// <summary>
    /// How long a session lasts without being used.
    /// </summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(60);

    /// <summary>
    /// How long a session lasts at most.
    /// </summary>
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromHours(12);

    /// <summary>
    /// How long a link to recover a password is valid.
    /// </summary>
    public static readonly TimeSpan RecoveryLifetime = TimeSpan.FromMinutes(30);

    private const int MaximumFailures = 5;

    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, Recovery> _recoveries = new();
    private readonly ConcurrentDictionary<string, Failures> _failures = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _lastRecoveryMail = new();

    /// <summary>
    /// Creates a session for an account.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="loginName">The login name.</param>
    /// <returns>The token of the session, for the cookie.</returns>
    public string CreateSession(Guid accountId, string loginName)
    {
        this.RemoveExpired();
        var token = NewToken();
        var now = DateTime.UtcNow;
        this._sessions[Hash(token)] = new Session(accountId, loginName, now, now);
        return token;
    }

    /// <summary>
    /// Gets the session of a token and extends it.
    /// </summary>
    /// <param name="token">The token of the cookie.</param>
    /// <returns>The id and login name of the account; <c>null</c> if there is no valid session.</returns>
    public (Guid AccountId, string LoginName)? GetSession(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 100)
        {
            return null;
        }

        var key = Hash(token);
        if (!this._sessions.TryGetValue(key, out var session))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (now - session.LastUse > IdleTimeout || now - session.Created > MaximumLifetime)
        {
            this._sessions.TryRemove(key, out _);
            return null;
        }

        this._sessions[key] = session with { LastUse = now };
        return (session.AccountId, session.LoginName);
    }

    /// <summary>
    /// Ends the session of a token.
    /// </summary>
    /// <param name="token">The token of the cookie.</param>
    public void EndSession(string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            this._sessions.TryRemove(Hash(token), out _);
        }
    }

    /// <summary>
    /// Ends all sessions of an account, e.g. after its password changed.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="exceptToken">The token of a session which stays, if any.</param>
    public void EndSessionsOf(Guid accountId, string? exceptToken = null)
    {
        var kept = string.IsNullOrEmpty(exceptToken) ? null : Hash(exceptToken);
        foreach (var entry in this._sessions)
        {
            if (entry.Value.AccountId == accountId && entry.Key != kept)
            {
                this._sessions.TryRemove(entry.Key, out _);
            }
        }

        foreach (var entry in this._recoveries)
        {
            if (entry.Value.AccountId == accountId)
            {
                this._recoveries.TryRemove(entry.Key, out _);
            }
        }
    }

    /// <summary>
    /// Gets whether too many logins failed lately for a key (a login name or an IP address).
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c>, if it's blocked for now.</returns>
    public bool IsBlocked(string key)
    {
        return this._failures.TryGetValue(key, out var failures)
            && failures.Count >= MaximumFailures
            && DateTime.UtcNow - failures.First < FailureWindow;
    }

    /// <summary>
    /// Counts a failed login for a key.
    /// </summary>
    /// <param name="key">The key.</param>
    public void AddFailure(string key)
    {
        var now = DateTime.UtcNow;
        this._failures.AddOrUpdate(
            key,
            _ => new Failures(now, 1),
            (_, old) => now - old.First >= FailureWindow ? new Failures(now, 1) : old with { Count = old.Count + 1 });
    }

    /// <summary>
    /// Forgets the failed logins of a key, after a successful login.
    /// </summary>
    /// <param name="key">The key.</param>
    public void ClearFailures(string key) => this._failures.TryRemove(key, out _);

    /// <summary>
    /// Creates a link token to recover the password of an account, at most one every few minutes.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <returns>The token; <c>null</c> if the account got one a moment ago.</returns>
    public string? CreateRecovery(Guid accountId)
    {
        this.RemoveExpired();
        var now = DateTime.UtcNow;
        if (this._lastRecoveryMail.TryGetValue(accountId, out var last) && now - last < RecoveryInterval)
        {
            return null;
        }

        this._lastRecoveryMail[accountId] = now;
        var token = NewToken();
        this._recoveries[Hash(token)] = new Recovery(accountId, now + RecoveryLifetime);
        return token;
    }

    /// <summary>
    /// Gets the account of a recovery token, without using it up.
    /// </summary>
    /// <param name="token">The token of the link.</param>
    /// <returns>The id of the account; <c>null</c> if the token isn't valid.</returns>
    public Guid? GetRecovery(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 100
            || !this._recoveries.TryGetValue(Hash(token), out var recovery)
            || recovery.ValidUntil < DateTime.UtcNow)
        {
            return null;
        }

        return recovery.AccountId;
    }

    /// <summary>
    /// Uses up a recovery token.
    /// </summary>
    /// <param name="token">The token of the link.</param>
    public void EndRecovery(string token) => this._recoveries.TryRemove(Hash(token), out _);

    private static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private void RemoveExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in this._sessions)
        {
            if (now - entry.Value.LastUse > IdleTimeout || now - entry.Value.Created > MaximumLifetime)
            {
                this._sessions.TryRemove(entry.Key, out _);
            }
        }

        foreach (var entry in this._recoveries)
        {
            if (entry.Value.ValidUntil < now)
            {
                this._recoveries.TryRemove(entry.Key, out _);
            }
        }

        foreach (var entry in this._failures)
        {
            if (now - entry.Value.First >= FailureWindow)
            {
                this._failures.TryRemove(entry.Key, out _);
            }
        }

        foreach (var entry in this._lastRecoveryMail)
        {
            if (now - entry.Value >= RecoveryInterval)
            {
                this._lastRecoveryMail.TryRemove(entry.Key, out _);
            }
        }
    }

    private sealed record Session(Guid AccountId, string LoginName, DateTime Created, DateTime LastUse);

    private sealed record Recovery(Guid AccountId, DateTime ValidUntil);

    private sealed record Failures(DateTime First, int Count);
}
