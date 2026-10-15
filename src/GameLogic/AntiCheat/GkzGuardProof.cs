// <copyright file="GkzGuardProof.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// The proof of a heartbeat of the anti-cheat of the MU GKZ client: a hash of the challenge of the server and
/// the values of the heartbeat with a secret, so a bot or a packet editor can't send heartbeats without having
/// taken the client apart. The client computes the same (MuMain src/source/Security/GkzGuard.cpp).
/// </summary>
public static class GkzGuardProof
{
    /// <summary>
    /// The environment variable with the secret (8 hex digits). This repository is public, so it's not in the code.
    /// </summary>
    public const string SecretVariable = "GKZ_GUARD_SECRET";

    private static readonly Lazy<uint> CurrentSecret = new(() =>
        Environment.GetEnvironmentVariable(SecretVariable) is { Length: 8 } hex ? Convert.ToUInt32(hex, 16) : 0x4D554B5Au);

    /// <summary>
    /// Gets the secret of the server.
    /// </summary>
    public static uint Secret => CurrentSecret.Value;

    /// <summary>
    /// Computes the proof of a heartbeat.
    /// </summary>
    /// <param name="secret">The secret.</param>
    /// <param name="challenge">The challenge which the server sent to the client.</param>
    /// <param name="sequence">The sequence number of the heartbeat.</param>
    /// <param name="clientTime">The time of the client, in milliseconds since its anti-cheat started.</param>
    /// <param name="flags">The detections.</param>
    /// <param name="detail">The detail of the detections.</param>
    /// <returns>The proof.</returns>
    public static uint Compute(uint secret, uint challenge, uint sequence, uint clientTime, uint flags, uint detail)
    {
        var hash = secret;
        hash = Mix(hash, challenge);
        hash = Mix(hash, sequence);
        hash = Mix(hash, clientTime);
        hash = Mix(hash, flags);
        hash = Mix(hash, detail);
        return hash;
    }

    private static uint Mix(uint hash, uint value)
    {
        unchecked
        {
            hash ^= value;
            hash *= 0x9E3779B1u;
            hash ^= hash >> 15;
            hash *= 0x85EBCA77u;
            hash ^= hash >> 13;
            return hash;
        }
    }
}
