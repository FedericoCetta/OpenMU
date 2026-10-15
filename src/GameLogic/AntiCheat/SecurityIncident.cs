// <copyright file="SecurityIncident.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// A security incident.
/// </summary>
/// <param name="Time">The time (UTC).</param>
/// <param name="Account">The login name of the account.</param>
/// <param name="Character">The name of the character.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Detail">What happened.</param>
/// <param name="Action">What the server did.</param>
public sealed record SecurityIncident(DateTime Time, string Account, string Character, string Kind, string Detail, string Action);
