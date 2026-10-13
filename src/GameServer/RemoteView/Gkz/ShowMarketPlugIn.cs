// <copyright file="ShowMarketPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Gkz;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.GameLogic.Market;
using MUnique.OpenMU.GameLogic.Views.Market;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the market to the MU GKZ client.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Page (C2, EC 68): [page u16] [page count u16] [count] [offers].</item>
/// <item>Store (C2, EC 69): [open] [open seconds left u32] [fee u32] [pending zen u32] [pending coins u32] [maximum listings] [count]
/// [pending jewels: Bless, Soul, Chaos, Life, Creation, u32 each] [offers].</item>
/// <item>Result (C1, EC 6A): [result].</item>
/// <item>An offer: [listing id, 16] [price u32] [currency] [seconds left i32, negative when ended] [seller, 10] [own] [item length] [item data].</item>
/// </list>
/// </remarks>
[PlugIn]
[Display(Name = "MU GKZ: market view", Description = "Sends the market to the MU GKZ client.")]
[Guid("18B34569-A2CE-4401-B59D-E6273C84A50B")]
public class ShowMarketPlugIn : IShowMarketPlugIn
{
    /// <summary>The sub code of a page.</summary>
    internal const byte PageCode = 0x68;

    /// <summary>The sub code of the store.</summary>
    internal const byte StoreCode = 0x69;

    /// <summary>The sub code of a result.</summary>
    internal const byte ResultCode = 0x6A;

    private const int HeaderSize = 5;
    private const int NameLength = 10;
    private const int OfferHeaderSize = 16 + 4 + 1 + 4 + NameLength + 1 + 1;
    private const int PendingJewelsSize = 5 * 4;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowMarketPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowMarketPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowMarketPageAsync(IReadOnlyList<MarketOffer> offers, int page, int pageCount)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var serializer = this._player.ItemSerializer;
            var span = connection.Output.GetSpan(HeaderSize + 5 + (offers.Count * (OfferHeaderSize + serializer.NeededSpace)));
            var length = HeaderSize;
            BinaryPrimitives.WriteUInt16LittleEndian(span[length..], (ushort)page);
            BinaryPrimitives.WriteUInt16LittleEndian(span[(length + 2)..], (ushort)pageCount);
            span[length + 4] = (byte)offers.Count;
            length += 5;
            foreach (var offer in offers)
            {
                length += WriteOffer(span[length..], offer, serializer);
            }

            WriteHeader(span, length, PageCode);
            return length;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowMarketStoreAsync(MarketStoreState state)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var serializer = this._player.ItemSerializer;
            var offers = state.Listings.Take(byte.MaxValue).ToList();
            var span = connection.Output.GetSpan(HeaderSize + 19 + PendingJewelsSize + (offers.Count * (OfferHeaderSize + serializer.NeededSpace)));
            var length = HeaderSize;
            span[length] = state.IsOpen ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt32LittleEndian(span[(length + 1)..], (uint)Math.Max(0, state.OpenTimeLeft.TotalSeconds));
            BinaryPrimitives.WriteUInt32LittleEndian(span[(length + 5)..], (uint)state.StoreFee);
            BinaryPrimitives.WriteUInt32LittleEndian(span[(length + 9)..], (uint)Math.Min(state.PendingZen, uint.MaxValue));
            BinaryPrimitives.WriteUInt32LittleEndian(span[(length + 13)..], (uint)Math.Max(0, state.PendingCoins));
            span[length + 17] = (byte)state.MaximumListings;
            span[length + 18] = (byte)offers.Count;
            length += 19;
            foreach (var jewel in MarketJewels.Items.Keys)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span[length..], (uint)Math.Max(0, state.PendingJewels.GetValueOrDefault(jewel)));
                length += 4;
            }
            foreach (var offer in offers)
            {
                length += WriteOffer(span[length..], offer, serializer);
            }

            WriteHeader(span, length, StoreCode);
            return length;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowMarketResultAsync(MarketResult result)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        int WritePacket()
        {
            var span = connection.Output.GetSpan(5);
            span[0] = 0xC1;
            span[1] = 5;
            span[2] = 0xEC;
            span[3] = ResultCode;
            span[4] = (byte)result;
            return 5;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an offer.
    /// </summary>
    /// <param name="span">The target.</param>
    /// <param name="offer">The offer.</param>
    /// <param name="serializer">The item serializer of the client.</param>
    /// <returns>The written bytes.</returns>
    internal static int WriteOffer(Span<byte> span, MarketOffer offer, IItemSerializer serializer)
    {
        offer.ListingId.TryWriteBytes(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], (uint)Math.Clamp(offer.Price, 0, uint.MaxValue));
        span[20] = (byte)offer.Currency;
        BinaryPrimitives.WriteInt32LittleEndian(span[21..], (int)Math.Clamp(offer.TimeLeft.TotalSeconds, int.MinValue, int.MaxValue));
        var name = span.Slice(25, NameLength);
        name.Clear();
        Encoding.UTF8.GetBytes(offer.Seller.AsSpan(0, Math.Min(offer.Seller.Length, NameLength)), name);
        span[25 + NameLength] = offer.IsOwn ? (byte)1 : (byte)0;
        var itemLength = serializer.SerializeItem(span[(OfferHeaderSize)..], offer.Item);
        span[OfferHeaderSize - 1] = (byte)itemLength;
        return OfferHeaderSize + itemLength;
    }

    private static void WriteHeader(Span<byte> span, int length, byte subCode)
    {
        span[0] = 0xC2;
        BinaryPrimitives.WriteUInt16BigEndian(span[1..], (ushort)length);
        span[3] = 0xEC;
        span[4] = subCode;
    }
}
