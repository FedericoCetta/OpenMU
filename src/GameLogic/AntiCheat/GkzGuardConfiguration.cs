// <copyright file="GkzGuardConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// The configuration of the anti-cheat of the MU GKZ client.
/// </summary>
public class GkzGuardConfiguration
{
    /// <summary>
    /// Gets or sets the time after which a player in the game without a heartbeat is disconnected.
    /// The client sends one every 10 seconds; a client without the anti-cheat (e.g. a bot) sends none.
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Gets or sets how far the clock of the client may run ahead of the clock of the server, in milliseconds,
    /// before it counts as a speedhack. Network delays only make the client clock appear behind, never ahead.
    /// </summary>
    public int MaximumClockLeadMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets a value indicating whether a player is disconnected when the anti-cheat detects something.
    /// </summary>
    public bool DisconnectOnDetection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the account is banned when the anti-cheat detects a speedhack,
    /// a changed code or a forged heartbeat. Forbidden programs only disconnect, because a player may have
    /// them open by mistake.
    /// </summary>
    public bool BanOnDetection { get; set; }

    /// <summary>
    /// Gets or sets the URL of a Discord webhook (channel settings, integrations, webhooks), to which the
    /// security incidents are sent; empty for none. Only discord.com addresses are used.
    /// </summary>
    public string? DiscordWebhookUrl { get; set; }
}
