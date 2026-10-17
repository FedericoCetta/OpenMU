// <copyright file="GkzNews.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Web;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Page which edits the news of the website and the launcher of MU GKZ. They are kept in the configuration
/// of the <see cref="GkzWebNewsPlugIn"/>, so saving them reaches the running game right away.
/// </summary>
public partial class GkzNews
{
    private List<GkzNewsItem>? _items;

    private GkzNewsItem? _editing;

    private int _editingIndex = -1;

    private int _confirmDeleteIndex = -1;

    private bool _isBusy;

    private string? _message;

    private string? _error;

    /// <summary>
    /// Gets or sets the data source of the game configuration.
    /// </summary>
    [Inject]
    public IDataSource<GameConfiguration> DataSource { get; set; } = null!;

    /// <summary>
    /// Gets or sets the persistence context provider.
    /// </summary>
    [Inject]
    public IPersistenceContextProvider PersistenceContextProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    [Inject]
    public ILogger<GkzNews> Logger { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync().ConfigureAwait(true);
        try
        {
            var configuration = await this.GetPlugInConfigurationAsync().ConfigureAwait(true);
            if (configuration is null)
            {
                this._error = "El plugin \"MU GKZ: noticias\" todavía no existe en la base. Se crea solo al reiniciar el server.";
                return;
            }

            var news = configuration.GetConfiguration<GkzWebNewsConfiguration>(new ByDataSourceReferenceHandler(this.DataSource))
                       ?? (GkzWebNewsConfiguration)new GkzWebNewsPlugIn().CreateDefaultConfig();
            this._items = news.Items.Select(Copy).ToList();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error al cargar las noticias.");
            this._error = "No se pudieron cargar las noticias: " + ex.Message;
        }
    }

    private static GkzNewsItem Copy(GkzNewsItem item) => new()
    {
        Title = item.Title,
        Date = item.Date,
        Tag = item.Tag,
        Text = item.Text,
        IsPublished = item.IsPublished,
    };

    private async Task<PlugInConfiguration?> GetPlugInConfigurationAsync()
    {
        var gameConfiguration = await this.DataSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
        var configuration = gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == GkzWebNewsPlugIn.Id);
        if (configuration is null)
        {
            return null;
        }

        // The data source keeps what it loaded first; the persisted value is the current one.
        using var context = this.PersistenceContextProvider.CreateNewTypedContext(typeof(PlugInConfiguration), false);
        if (await context.GetByIdAsync<PlugInConfiguration>(configuration.GetId()).ConfigureAwait(true) is { } persisted
            && persisted.CustomConfiguration != configuration.CustomConfiguration)
        {
            configuration.CustomConfiguration = persisted.CustomConfiguration;
        }

        return configuration;
    }

    private void OnNew()
    {
        this._message = null;
        this._editingIndex = -1;
        this._editing = new GkzNewsItem { Date = DateTime.Today };
    }

    private void OnEdit(int index)
    {
        this._message = null;
        this._confirmDeleteIndex = -1;
        this._editingIndex = index;
        this._editing = Copy(this._items![index]);
    }

    private void OnCancelEditing()
    {
        this._editing = null;
        this._editingIndex = -1;
    }

    private async Task OnSaveEditingAsync()
    {
        if (this._editing is not { } editing || this._items is null)
        {
            return;
        }

        editing.Title = (editing.Title ?? string.Empty).Trim();
        editing.Text = (editing.Text ?? string.Empty).Trim();
        if (editing.Title.Length == 0 || editing.Text.Length == 0)
        {
            this._error = "La noticia necesita un título y un texto.";
            return;
        }

        if (editing.Title.Length > GkzNewsLimits.TitleLength || editing.Text.Length > GkzNewsLimits.TextLength)
        {
            this._error = $"El título puede tener hasta {GkzNewsLimits.TitleLength} caracteres y el texto hasta {GkzNewsLimits.TextLength}.";
            return;
        }

        if (!GkzNewsLimits.Tags.Contains(editing.Tag))
        {
            editing.Tag = GkzNewsLimits.Tags[0];
        }

        var items = this._items.Select(Copy).ToList();
        if (this._editingIndex < 0)
        {
            items.Insert(0, editing);
        }
        else
        {
            items[this._editingIndex] = editing;
        }

        if (await this.SaveAsync(items, "Noticia guardada.").ConfigureAwait(true))
        {
            this._editing = null;
            this._editingIndex = -1;
        }
    }

    private async Task OnMoveAsync(int index, int direction)
    {
        var target = index + direction;
        if (this._items is null || target < 0 || target >= this._items.Count)
        {
            return;
        }

        var items = this._items.Select(Copy).ToList();
        (items[index], items[target]) = (items[target], items[index]);
        await this.SaveAsync(items, "Orden guardado.").ConfigureAwait(true);
    }

    private async Task OnDeleteAsync(int index)
    {
        this._confirmDeleteIndex = -1;
        if (this._items is null)
        {
            return;
        }

        var items = this._items.Select(Copy).ToList();
        items.RemoveAt(index);
        if (await this.SaveAsync(items, "Noticia borrada.").ConfigureAwait(true) && this._editingIndex >= 0)
        {
            this.OnCancelEditing();
        }
    }

    private async Task<bool> SaveAsync(List<GkzNewsItem> items, string message)
    {
        this._isBusy = true;
        this._error = null;
        this._message = null;
        try
        {
            var configuration = await this.GetPlugInConfigurationAsync().ConfigureAwait(true);
            if (configuration is null)
            {
                this._error = "El plugin \"MU GKZ: noticias\" todavía no existe en la base. Se crea solo al reiniciar el server.";
                return false;
            }

            configuration.SetConfiguration(new GkzWebNewsConfiguration { Items = items }, new ByDataSourceReferenceHandler(this.DataSource));
            await (await this.DataSource.GetContextAsync().ConfigureAwait(true)).SaveChangesAsync().ConfigureAwait(true);
            this._items = items;
            this._message = message;
            return true;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error al guardar las noticias.");
            this._error = "No se pudieron guardar las noticias: " + ex.Message;
            return false;
        }
        finally
        {
            this._isBusy = false;
        }
    }
}
