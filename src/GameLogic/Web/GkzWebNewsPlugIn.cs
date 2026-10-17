// <copyright file="GkzWebNewsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Web;

using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A news entry of the website and the launcher of MU GKZ.
/// </summary>
public class GkzNewsItem
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date which is shown.
    /// </summary>
    [Display(Name = "Fecha")]
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Gets or sets the tag, e.g. "Novedad", "Evento" or "Servidor". It also selects the picture on the website.
    /// </summary>
    [Display(Name = "Etiqueta")]
    public string Tag { get; set; } = "Novedad";

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    [Display(Name = "Texto")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the entry is published.
    /// </summary>
    [Display(Name = "Publicada")]
    public bool IsPublished { get; set; } = true;
}

/// <summary>
/// The limits of the news, which the admin page applies.
/// </summary>
public static class GkzNewsLimits
{
    /// <summary>
    /// The longest title.
    /// </summary>
    public const int TitleLength = 100;

    /// <summary>
    /// The longest text.
    /// </summary>
    public const int TextLength = 1500;

    /// <summary>
    /// Gets the tags which can be chosen; the website shows a picture by tag.
    /// </summary>
    public static IReadOnlyList<string> Tags { get; } = ["Novedad", "Evento", "Servidor", "Mantenimiento"];
}

/// <summary>
/// The news of the website and the launcher of MU GKZ, in the order in which they are shown.
/// </summary>
public class GkzWebNewsConfiguration
{
    /// <summary>
    /// Gets or sets the news, the first one is shown first.
    /// </summary>
    [Display(Name = "Noticias")]
    public List<GkzNewsItem> Items { get; set; } = [];
}

/// <summary>
/// Keeps the news of the website and the launcher of MU GKZ. They are edited on the page "Noticias" of
/// the admin panel and are published right away, without a restart.
/// </summary>
[PlugIn]
[Display(Name = "MU GKZ: noticias", Description = "Las noticias de la web y del launcher. Se editan en la página Noticias del admin.")]
[Guid("8E4C2B7D-5A19-4F3E-9C61-2D7B0A8F4E52")]
public class GkzWebNewsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<GkzWebNewsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The id of the plugin, to find its configuration.
    /// </summary>
    public static readonly Guid Id = new("8E4C2B7D-5A19-4F3E-9C61-2D7B0A8F4E52");

    /// <inheritdoc />
    public GkzWebNewsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the published news of a game server.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The published news, in order; empty when the plugin isn't active.</returns>
    public static IReadOnlyList<GkzNewsItem> GetPublishedNews(IGameContext gameContext)
        => gameContext.FeaturePlugIns.GetPlugIn<GkzWebNewsPlugIn>()?.Configuration?.Items.Where(n => n.IsPublished).ToList()
           ?? [];

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GkzWebNewsConfiguration
    {
        // The news which the website and the launcher had in news.json before.
        Items =
        [
            new() { Title = "¡Bienvenidos a la beta cerrada!", Date = new DateTime(2026, 10, 12), Tag = "Novedad", Text = "MU GKZ abre sus puertas a los amigos de confianza. Reportá cualquier bug que encuentres." },
            new() { Title = "Nuevo Ring PvP en Lorencia", Date = new DateTime(2026, 10, 12), Tag = "Evento", Text = "La fuente de Lorencia ahora es un ring de combate. Adentro del ring se puede pelear; afuera sigue siendo zona segura." },
            new() { Title = "Season 3 clásica", Date = new DateTime(2026, 10, 11), Tag = "Servidor", Text = "5 clases clásicas, 3ra clase sin Master Level, alas de 3ra y Kanturu." },
        ],
    };
}
