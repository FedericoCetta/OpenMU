// <copyright file="LogInHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Login;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.Network.Xor;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for login packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.LogInHandlerPlugIn_Name), Description = nameof(PlugInResources.LogInHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("4A816FE5-809B-4D42-AF9F-1929FABD3295")]
[BelongsToGroup(LogInOutGroup.GroupKey)]
public class LogInHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly ISpanDecryptor _decryptor = new Xor3Decryptor(0);

    private readonly LoginAction _loginAction = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => true;

    /// <inheritdoc/>
    public byte Key => LoginLongPassword.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 42)
        {
            if (packet.Length > 28 + 3)
            {
                LoginShortPassword message = packet;
                if (!IsClientSerialValid(player, message.ClientSerial))
                {
                    await RejectWrongClientAsync(player).ConfigureAwait(false);
                    return;
                }

                await this.HandleLoginAsync(player, this.Decrypt(message.Username), this.Decrypt(message.Password), message.TickCount, ClientVersionResolver.Resolve(message.ClientVersion)).ConfigureAwait(false);
            }
            else
            {
                // we have some version like 0.75 which just uses three bytes as version identifier
                Login075 message = packet;
                await this.HandleLoginAsync(player, this.Decrypt(message.Username), this.Decrypt(message.Password), message.TickCount, ClientVersionResolver.Resolve(message.ClientVersion)).ConfigureAwait(false);
            }
        }
        else
        {
            LoginLongPassword message = packet;
            if (!IsClientSerialValid(player, message.ClientSerial))
            {
                await RejectWrongClientAsync(player).ConfigureAwait(false);
                return;
            }

            await this.HandleLoginAsync(player, this.Decrypt(message.Username), this.Decrypt(message.Password), message.TickCount, ClientVersionResolver.Resolve(message.ClientVersion)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks the serial of the game client against the serial which is configured for the client
    /// of the endpoint. Game clients with another serial (e.g. modified or foreign clients) are rejected.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="clientSerial">The serial which was sent by the client.</param>
    /// <returns><c>true</c>, if the login may continue.</returns>
    private static bool IsClientSerialValid(Player player, ReadOnlySpan<byte> clientSerial)
    {
        return player is not RemotePlayer { ExpectedClientSerial: { Length: > 0 } expected }
               || clientSerial.SequenceEqual(expected);
    }

    private static async ValueTask RejectWrongClientAsync(Player player)
    {
        player.Logger.LogWarning("Login rejected: the client serial doesn't match the configured serial. Address: {address}", (player as IHasIpAddress)?.IpAddress);
        await player.InvokeViewPlugInAsync<IShowLoginResultPlugIn>(p => p.ShowLoginResultAsync(LoginResult.WrongVersion)).ConfigureAwait(false);
    }

    private string Decrypt(Span<byte> stringSpan)
    {
        this._decryptor.Decrypt(stringSpan);
        return stringSpan.ExtractString(0, stringSpan.Length, Encoding.UTF8);
    }

    private async ValueTask HandleLoginAsync(Player player, string username, string password, uint tickCount, ClientVersion version)
    {
        if (player.Logger.IsEnabled(LogLevel.Debug))
        {
            player.Logger.LogDebug($"User tries to log in. username:{username}, version:{version}, tickCount:{tickCount} ");
        }

        await this._loginAction.LoginAsync(player, username, password).ConfigureAwait(false);
        if (player is RemotePlayer remotePlayer)
        {
            // Set Version in RemotePlayer so that the right plugins will be selected
            remotePlayer.ClientVersion = version;
        }
    }
}