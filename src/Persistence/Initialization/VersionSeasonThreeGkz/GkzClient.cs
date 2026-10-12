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
    /// The serial of the MU GKZ client. It has to match <c>Serial</c> in
    /// MuMain src/source/Network/Server/WSclient.cpp; the server rejects logins with another serial.
    /// </summary>
    internal static ReadOnlySpan<byte> Serial => "YVUa7B2ypOxlGoKe"u8;

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
