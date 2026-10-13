// <copyright file="ShowItemPostPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.ItemPost;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends a posted item to the MU GKZ client (0xEC, 0x51): C1 [length] EC 51 [sender name, 10 bytes] [item data].
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: item post view", Description = "Sends an item which a player posted to the chat to the MU GKZ client.")]
[Guid("C4A1E7B2-93D5-4F68-8E0A-6B2F1D9C7E35")]
public class ShowItemPostPlugIn : IShowItemPostPlugIn
{
    /// <summary>
    /// The size of the header, before the item data.
    /// </summary>
    internal const int HeaderSize = 4 + NameLength;

    private const int NameLength = 10;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowItemPostPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowItemPostPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowItemPostAsync(string sender, Item item)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var itemSerializer = this._player.ItemSerializer;
            var span = connection.Output.GetSpan(HeaderSize + itemSerializer.NeededSpace);
            span[0] = 0xC1;
            span[2] = 0xEC;
            span[3] = 0x51;
            var name = span.Slice(4, NameLength);
            name.Clear();
            Encoding.UTF8.GetBytes(sender.AsSpan(0, Math.Min(sender.Length, NameLength)), name);
            var length = HeaderSize + itemSerializer.SerializeItem(span[HeaderSize..], item);
            span[1] = (byte)length;
            return length;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }
}
