// <copyright file="ShowJewelBankPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views.JewelBank;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The implementation of the <see cref="IShowJewelBankPlugIn"/> for the MU GKZ client (0xEC 0x31).
/// </summary>
/// <remarks>
/// C1 [size] EC 31 [auto save] [entry count], then per entry: [group] [number u16] [level] [count u32].
/// The layout must match GkzPackets.h in MuMain.
/// </remarks>
[PlugIn]
[Display(Name = "MU GKZ: jewel bank view", Description = "Sends the jewel bank to the MU GKZ client.")]
[Guid("1F6C2D4E-F017-428B-B4C5-D0F102344CCB")]
public class ShowJewelBankPlugIn : IShowJewelBankPlugIn
{
    /// <summary>
    /// The size of one entry.
    /// </summary>
    internal const int EntrySize = 8;

    /// <summary>
    /// The size of the header, before the entries.
    /// </summary>
    internal const int HeaderSize = 6;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowJewelBankPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowJewelBankPlugIn(RemotePlayer player) => this._player = player;

    /// <summary>
    /// Writes the packet.
    /// </summary>
    /// <param name="span">The target.</param>
    /// <param name="items">The items.</param>
    /// <param name="autoSave">Whether the bank saves automatically.</param>
    /// <returns>The length of the packet.</returns>
    internal static int Write(Span<byte> span, IReadOnlyList<(JewelBankEntry Entry, int Count)> items, bool autoSave)
    {
        var count = Math.Min(items.Count, JewelBankConfiguration.MaximumEntries);
        var length = HeaderSize + (count * EntrySize);
        span[0] = 0xC1;
        span[1] = (byte)length;
        span[2] = 0xEC;
        span[3] = 0x31;
        span[4] = autoSave ? (byte)1 : (byte)0;
        span[5] = (byte)count;
        for (var i = 0; i < count; i++)
        {
            var (entry, pieces) = items[i];
            var entrySpan = span.Slice(HeaderSize + (i * EntrySize), EntrySize);
            entrySpan[0] = entry.Group;
            BinaryPrimitives.WriteInt16LittleEndian(entrySpan[1..], entry.Number);
            entrySpan[3] = entry.Level;
            BinaryPrimitives.WriteUInt32LittleEndian(entrySpan[4..], (uint)Math.Max(0, pieces));
        }

        return length;
    }

    /// <inheritdoc />
    public async ValueTask ShowJewelBankAsync(IReadOnlyList<(JewelBankEntry Entry, int Count)> items, bool autoSave)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var maximumLength = HeaderSize + (JewelBankConfiguration.MaximumEntries * EntrySize);
            return Write(connection.Output.GetSpan(maximumLength)[..maximumLength], items, autoSave);
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }
}
