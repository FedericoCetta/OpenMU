// <copyright file="GkzSecurityIncidents.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

/// <summary>
/// The security incidents of MU GKZ (anti-cheat, speedhack, packet floods, dupe attempts): kept for the
/// admin panel (the last ones in memory and all of them in logs/gkz-security.jsonl) and sent to a Discord
/// channel, if a webhook is configured (see <see cref="GkzGuardConfiguration.DiscordWebhookUrl"/>).
/// The game servers and the admin panel run in the same process, so they share this.
/// </summary>
public static class GkzSecurityIncidents
{
    /// <summary>
    /// How many incidents are kept in memory.
    /// </summary>
    public const int MaximumKept = 500;

    /// <summary>
    /// The time in which the same kind of incident of the same account goes to Discord only once.
    /// </summary>
    public static readonly TimeSpan DiscordDebounce = TimeSpan.FromMinutes(5);

    private static readonly object SyncRoot = new();
    private static readonly LinkedList<SecurityIncident> Recent = new();
    private static readonly ConcurrentDictionary<string, DateTime> LastDiscordMessages = new();
    private static readonly Channel<string> DiscordMessages = Channel.CreateBounded<string>(new BoundedChannelOptions(200) { FullMode = BoundedChannelFullMode.DropOldest });
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private static bool _loaded;
    private static int _senderStarted;

    /// <summary>
    /// Gets or sets the path of the file with all incidents; <c>null</c> to keep them only in memory (tests).
    /// </summary>
    public static string? FilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "logs", "gkz-security.jsonl");

    /// <summary>
    /// Gets or sets the URL of the Discord webhook, or <c>null</c>.
    /// </summary>
    public static string? DiscordWebhookUrl { get; set; }

    /// <summary>
    /// Gets the recent incidents, the newest first.
    /// </summary>
    /// <returns>The incidents.</returns>
    public static IReadOnlyList<SecurityIncident> GetRecent()
    {
        lock (SyncRoot)
        {
            EnsureLoaded();
            return Recent.ToList();
        }
    }

    /// <summary>
    /// Reports an incident of a player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="kind">The kind, e.g. "Anti-cheat" or "Speedhack".</param>
    /// <param name="detail">What happened.</param>
    /// <param name="action">What the server did, e.g. "disconnected".</param>
    public static void Report(Player player, string kind, string detail, string action)
    {
        Report(new SecurityIncident(DateTime.UtcNow, player.Account?.LoginName ?? string.Empty, player.SelectedCharacter?.Name ?? string.Empty, kind, detail, action));
    }

    /// <summary>
    /// Reports an incident.
    /// </summary>
    /// <param name="incident">The incident.</param>
    public static void Report(SecurityIncident incident)
    {
        lock (SyncRoot)
        {
            EnsureLoaded();
            Recent.AddFirst(incident);
            while (Recent.Count > MaximumKept)
            {
                Recent.RemoveLast();
            }

            if (FilePath is { } path)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.AppendAllText(path, JsonSerializer.Serialize(incident, JsonOptions) + Environment.NewLine);
                }
                catch (IOException)
                {
                    // The log is a convenience; the incident is still in memory and in the normal log.
                }
            }
        }

        QueueForDiscord(incident);
    }

    /// <summary>
    /// Clears the incidents in memory (tests).
    /// </summary>
    internal static void ClearForTests()
    {
        lock (SyncRoot)
        {
            Recent.Clear();
            LastDiscordMessages.Clear();
            _loaded = true;
        }
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (FilePath is not { } path || !File.Exists(path))
        {
            return;
        }

        try
        {
            foreach (var line in File.ReadLines(path).Reverse().Take(MaximumKept).Reverse())
            {
                if (JsonSerializer.Deserialize<SecurityIncident>(line) is { } incident)
                {
                    Recent.AddFirst(incident);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            // A broken log doesn't stop the server.
        }
    }

    private static void QueueForDiscord(SecurityIncident incident)
    {
        if (string.IsNullOrWhiteSpace(DiscordWebhookUrl))
        {
            return;
        }

        var key = $"{incident.Account}|{incident.Kind}";
        var now = DateTime.UtcNow;
        if (LastDiscordMessages.TryGetValue(key, out var last) && now - last < DiscordDebounce)
        {
            return;
        }

        LastDiscordMessages[key] = now;
        DiscordMessages.Writer.TryWrite($"🚨 **{incident.Kind}** - cuenta `{incident.Account}`, personaje `{incident.Character}`: {incident.Detail} → {incident.Action}");
        if (Interlocked.Exchange(ref _senderStarted, 1) == 0)
        {
            _ = Task.Run(SendToDiscordAsync);
        }
    }

    private static async Task SendToDiscordAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await foreach (var message in DiscordMessages.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            if (DiscordWebhookUrl is not { } url || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host is not ("discord.com" or "discordapp.com"))
            {
                continue;
            }

            try
            {
                using var response = await client.PostAsJsonAsync(uri, new { content = message[..Math.Min(message.Length, 1900)] }).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Discord isn't reachable; the incident is in the panel anyway.
            }

            // Discord allows a few messages per second per webhook.
            await Task.Delay(1500).ConfigureAwait(false);
        }
    }
}
