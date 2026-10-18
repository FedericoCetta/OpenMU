// <copyright file="GkzJewelRainConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the jewel rain of MU GKZ.
/// </summary>
public class GkzJewelRainConfiguration : PeriodicTaskConfiguration
{
    /// <summary>
    /// Gets the default configuration: every hour at minute 30 (in the time zone of the server), for 2 minutes,
    /// around the PvP ring of Lorencia, announced 3 minutes before (1 minute when it's started by /lluvia).
    /// </summary>
    public static GkzJewelRainConfiguration Default => new()
    {
        PreStartMessageDelay = TimeSpan.FromMinutes(3),
        ForcedPreStartMessageDelay = TimeSpan.FromMinutes(1),
        TaskDuration = TimeSpan.FromMinutes(2),
        StartMessage = "¡Lluvia de joyas en 3 minutos en Lorencia!",
        ForcedStartMessage = "¡Lluvia de joyas en 1 minuto en Lorencia!",
        EndMessage = "Terminó la lluvia de joyas.",
        RainStartedMessage = "¡Empezó la lluvia de joyas en Lorencia!",
        Timetable = GenerateTimeSequence(TimeSpan.FromHours(1), new TimeOnly(0, 27)).ToList(),
        MapNumber = 0,
        CenterX = 141,
        CenterY = 128,
        Radius = 9,
        JewelCount = 40,
        Jewels = new List<GkzJewelRainItem>
        {
            new() { Group = 14, Number = 13, Weight = 35 }, // Jewel of Bless
            new() { Group = 14, Number = 14, Weight = 30 }, // Jewel of Soul
            new() { Group = 12, Number = 15, Weight = 27 }, // Jewel of Chaos
            new() { Group = 14, Number = 16, Weight = 8 }, // Jewel of Life
        },
    };

    /// <summary>
    /// Gets or sets the time between the message and the rain when it's started by /lluvia.
    /// </summary>
    [Display(Name = "Aviso previo con /lluvia", Order = 8)]
    public TimeSpan ForcedPreStartMessageDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the message which is shown when the rain is started by /lluvia.
    /// </summary>
    [Display(Name = "Mensaje con /lluvia", Order = 9)]
    public string ForcedStartMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message which is shown when the jewels start to fall.
    /// </summary>
    [Display(Name = "Mensaje al empezar a caer", Order = 10)]
    public string RainStartedMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of the map.
    /// </summary>
    [Display(Name = "Mapa (0 = Lorencia)", Order = 11)]
    public short MapNumber { get; set; }

    /// <summary>
    /// Gets or sets the x coordinate of the center of the rain.
    /// </summary>
    [Display(Name = "Centro X", Order = 12)]
    public byte CenterX { get; set; }

    /// <summary>
    /// Gets or sets the y coordinate of the center of the rain.
    /// </summary>
    [Display(Name = "Centro Y", Order = 13)]
    public byte CenterY { get; set; }

    /// <summary>
    /// Gets or sets the radius around the center, in coordinates.
    /// </summary>
    [Display(Name = "Radio", Order = 14)]
    public byte Radius { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels which fall during the whole rain, evenly spread over its duration.
    /// </summary>
    [Display(Name = "Cantidad de joyas", Order = 15)]
    public int JewelCount { get; set; }

    /// <summary>
    /// Gets or sets the jewels which can fall, with their weights.
    /// </summary>
    [Display(Name = "Joyas (grupo, número y peso)", Order = 16)]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<GkzJewelRainItem> Jewels { get; set; } = new List<GkzJewelRainItem>();
}

/// <summary>
/// An item which can fall during the jewel rain of MU GKZ.
/// </summary>
public class GkzJewelRainItem
{
    /// <summary>
    /// Gets or sets the group of the item.
    /// </summary>
    [Display(Name = "Grupo")]
    public byte Group { get; set; }

    /// <summary>
    /// Gets or sets the number of the item.
    /// </summary>
    [Display(Name = "Número")]
    public short Number { get; set; }

    /// <summary>
    /// Gets or sets the weight: the higher, the more often it falls compared to the others.
    /// </summary>
    [Display(Name = "Peso")]
    public int Weight { get; set; }
}
