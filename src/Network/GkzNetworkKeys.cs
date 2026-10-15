// <copyright file="GkzNetworkKeys.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network;

using System.IO;
using System.IO.Pipelines;
using System.Text.Json;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.Network.SimpleModulus;
using MUnique.OpenMU.Network.Xor;

/// <summary>
/// The own encryption keys of MU GKZ, so generic MU bots and packet editors, which use the well known
/// default keys, can't read or forge the traffic of the game servers.
/// </summary>
/// <remarks>
/// The keys aren't in the source code, because this repository is public: the server reads them from the
/// JSON file named by the environment variable <see cref="FileVariable"/>. Without it, the default keys are
/// used (development). If the variable is set, but the file can't be read, the server fails instead of
/// falling back to the public keys.
/// </remarks>
public sealed class GkzNetworkKeys
{
    /// <summary>
    /// The environment variable with the path of the key file.
    /// </summary>
    public const string FileVariable = "GKZ_NETWORK_KEYS_FILE";

    private static readonly Lazy<GkzNetworkKeys?> CurrentKeys = new(Load);

    /// <summary>
    /// Gets the keys of the server, or <c>null</c> if the default keys are used.
    /// </summary>
    public static GkzNetworkKeys? Current => CurrentKeys.Value;

    /// <summary>
    /// Gets or sets the 12 key values with which the client encrypts its packets (C3/C4).
    /// </summary>
    public uint[] ClientToServerEncrypt { get; set; } = [];

    /// <summary>
    /// Gets or sets the 12 key values with which the server decrypts the packets of the client (C3/C4).
    /// </summary>
    public uint[] ClientToServerDecrypt { get; set; } = [];

    /// <summary>
    /// Gets or sets the 12 key values with which the server encrypts its packets (C3/C4).
    /// </summary>
    public uint[] ServerToClientEncrypt { get; set; } = [];

    /// <summary>
    /// Gets or sets the 12 key values with which the client decrypts the packets of the server (C3/C4).
    /// </summary>
    public uint[] ServerToClientDecrypt { get; set; } = [];

    /// <summary>
    /// Gets or sets the 32 bytes of the Xor32 key, with which the client additionally encrypts all its packets.
    /// </summary>
    public byte[] Xor32 { get; set; } = [];

    /// <summary>
    /// Creates the encryptor of the given direction.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>The encryptor.</returns>
    public IPipelinedEncryptor CreateEncryptor(PipeWriter target, DataDirection direction)
    {
        return direction == DataDirection.ServerToClient
            ? new PipelinedEncryptor(target, this.ServerToClientEncrypt)
            : new PipelinedXor32Encryptor(new PipelinedSimpleModulusEncryptor(target, this.ClientToServerEncrypt).Writer, this.Xor32);
    }

    /// <summary>
    /// Creates the decryptor of the given direction.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>The decryptor.</returns>
    public IPipelinedDecryptor CreateDecryptor(PipeReader source, DataDirection direction)
    {
        return direction == DataDirection.ClientToServer
            ? new PipelinedDecryptor(source, SimpleModulusKeys.CreateDecryptionKeys(this.ClientToServerDecrypt), this.Xor32)
            : new PipelinedSimpleModulusDecryptor(source, this.ServerToClientDecrypt);
    }

    private static GkzNetworkKeys? Load()
    {
        var path = Environment.GetEnvironmentVariable(FileVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // The Xor32 key is written as a list of numbers, not as base64.
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var keys = new GkzNetworkKeys
        {
            ClientToServerEncrypt = ReadKey(root, "clientToServerEncrypt"),
            ClientToServerDecrypt = ReadKey(root, "clientToServerDecrypt"),
            ServerToClientEncrypt = ReadKey(root, "serverToClientEncrypt"),
            ServerToClientDecrypt = ReadKey(root, "serverToClientDecrypt"),
            Xor32 = root.GetProperty("xor32").EnumerateArray().Select(e => e.GetByte()).ToArray(),
        };
        keys.Validate(path);
        return keys;
    }

    private static uint[] ReadKey(JsonElement root, string name)
    {
        return root.GetProperty(name).EnumerateArray().Select(e => e.GetUInt32()).ToArray();
    }

    private void Validate(string path)
    {
        foreach (var key in new[] { this.ClientToServerEncrypt, this.ClientToServerDecrypt, this.ServerToClientEncrypt, this.ServerToClientDecrypt })
        {
            if (key.Length != 12)
            {
                throw new InvalidOperationException($"The key file {path} needs 12 values for each simple modulus key.");
            }
        }

        if (this.Xor32.Length != 32)
        {
            throw new InvalidOperationException($"The key file {path} needs 32 bytes for the Xor32 key.");
        }
    }
}
