// <copyright file="GkzClient.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using MUnique.OpenMU.Network.PlugIns;

/// <summary>
/// The game client of MU GKZ: the open source client (MuMain) with an own serial.
/// It's the only client which is accepted, so the original client and its public hacks can't connect.
/// </summary>
internal static class GkzClient
{
    /// <summary>
    /// The season number of the open source Season 6 Episode 3 client.
    /// </summary>
    internal const byte Season = 106;

    /// <summary>
    /// The port of the connect server of the MU GKZ client.
    /// </summary>
    internal const int ConnectServerPort = 44405;

    /// <summary>
    /// The description of the client.
    /// </summary>
    internal const string Description = "MU GKZ Client (open source Season 6 Episode 3)";

    /// <summary>
    /// The environment variable with the serial of the MU GKZ client (16 characters).
    /// </summary>
    internal const string SerialVariable = "GKZ_CLIENT_SERIAL";

    /// <summary>
    /// Gets the serial of the MU GKZ client. It has to match <c>Serial</c> in
    /// MuMain src/source/Network/Server/WSclient.cpp; the server rejects logins with another serial.
    /// </summary>
    /// <remarks>
    /// This repository is public, so the real serial comes from the environment variable <see cref="SerialVariable"/>.
    /// The fallback is the former serial, which is public and therefore not accepted by the current client.
    /// </remarks>
    internal static ReadOnlySpan<byte> Serial => Environment.GetEnvironmentVariable(SerialVariable) is { Length: 16 } serial
        ? System.Text.Encoding.ASCII.GetBytes(serial)
        : "YVUa7B2ypOxlGoKe"u8;

    /// <summary>
    /// The version of the client.
    /// </summary>
    internal static ReadOnlySpan<byte> Version => [0x32, 0x30, 0x34, 0x30, 0x34];

    /// <summary>
    /// Applies the MU GKZ client data to the definition.
    /// </summary>
    /// <param name="client">The client definition.</param>
    internal static void Apply(GameClientDefinition client)
    {
        client.Season = Season;
        client.Episode = 3;
        client.Language = ClientLanguage.English;
        client.Version = Version.ToArray();
        client.Serial = Serial.ToArray();
        client.Description = Description;
    }
}
