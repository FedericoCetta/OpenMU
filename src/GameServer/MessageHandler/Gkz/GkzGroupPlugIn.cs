// <copyright file="GkzGroupPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Gkz;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler for the packets of the MU GKZ client (0xEC identifier): the VIP menu and the vault selection.
/// </summary>
/// <remarks>
/// The packets are C1 packets: C1 [size] EC [sub code] [data]. They're known only to the MU GKZ client,
/// see GkzPackets.h in MuMain.
/// </remarks>
[PlugIn]
[Display(Name = "MU GKZ: client packets", Description = "Packet handler for the packets of the MU GKZ client (0xEC): VIP menu and vault selection.")]
[Guid("E4A7C1D2-3B5F-4C68-8A9E-1F2D3C4B5A60")]
internal class GkzGroupPlugIn : GroupPacketHandlerPlugIn
{
    /// <summary>
    /// The group key.
    /// </summary>
    internal const byte GroupKey = 0xEC;

    /// <summary>
    /// Initializes a new instance of the <see cref="GkzGroupPlugIn" /> class.
    /// </summary>
    /// <param name="clientVersionProvider">The client version provider.</param>
    /// <param name="manager">The manager.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public GkzGroupPlugIn(IClientVersionProvider clientVersionProvider, PlugInManager manager, ILoggerFactory loggerFactory)
        : base(clientVersionProvider, manager, loggerFactory)
    {
    }

    /// <inheritdoc/>
    public override bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public override byte Key => GroupKey;
}
