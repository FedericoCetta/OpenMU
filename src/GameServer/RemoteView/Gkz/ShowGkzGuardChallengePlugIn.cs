// <copyright file="ShowGkzGuardChallengePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.AntiCheat;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the challenge of the anti-cheat to the MU GKZ client: C1 08 EC 71 [challenge u32].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: anti-cheat challenge view", Description = "Sends the challenge of the anti-cheat to the MU GKZ client.")]
[Guid("8D1F4A62-2C7E-4B93-A5F0-7E3B9C1D6A28")]
public class ShowGkzGuardChallengePlugIn : IShowGkzGuardChallengePlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowGkzGuardChallengePlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowGkzGuardChallengePlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowChallengeAsync(uint challenge)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var span = connection.Output.GetSpan(8);
            span[0] = 0xC1;
            span[1] = 8;
            span[2] = 0xEC;
            span[3] = 0x71;
            BinaryPrimitives.WriteUInt32LittleEndian(span[4..], challenge);
            return 8;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }
}
