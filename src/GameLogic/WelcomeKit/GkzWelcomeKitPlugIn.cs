// <copyright file="GkzWelcomeKitPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.WelcomeKit;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The welcome kit of MU GKZ: during the first days of the server, new characters start with free points,
/// zen and first wings which expire. It also deletes the items whose time ran out.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7D1F4C2A-93E6-4B58-8A17-3C6E0F9B2D41")]
public class GkzWelcomeKitPlugIn : ICharacterCreatedPlugIn, IPeriodicTaskPlugIn, IPlayerStateChangedPlugIn, ISupportCustomConfiguration<GkzWelcomeKitConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "MU GKZ: kit de bienvenida";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Los personajes nuevos arrancan con puntos, zen y alas que vencen. También borra los ítems vencidos.";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private DateTime _nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public GkzWelcomeKitConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GkzWelcomeKitConfiguration();

    /// <inheritdoc />
    public void CharacterCreated(Account account, Character createdCharacter, IContext persistenceContext, GameConfiguration gameConfiguration, ILogger logger)
    {
        var configuration = this.Configuration ?? new GkzWelcomeKitConfiguration();
        if (DateTime.UtcNow >= configuration.EndUtc
            || account.WelcomeKitsClaimed >= configuration.MaximumKitsPerAccount
            || account.IsBot
            || account.IsTemplate
            || createdCharacter.Inventory is not { } inventory)
        {
            return;
        }

        account.WelcomeKitsClaimed++;
        createdCharacter.LevelUpPoints += configuration.Points;
        inventory.Money += configuration.Money;
        AddWings(createdCharacter, inventory, configuration, persistenceContext, gameConfiguration, logger);
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var now = DateTime.UtcNow;
        if (now < this._nextRunUtc)
        {
            return;
        }

        this._nextRunUtc = now + CheckInterval;
        foreach (var player in await gameContext.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.PlayerState.CurrentState == PlayerState.EnteredWorld)
            {
                await RemoveExpiredItemsAsync(player).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public void ForceStart() => this._nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld)
        {
            return;
        }

        await RemoveExpiredItemsAsync(player).ConfigureAwait(false);
        if (player.Inventory?.Items.Where(i => i.ExpiresAt is not null).MinBy(i => i.ExpiresAt) is { } item)
        {
            // The time left, because the server's time zone isn't the player's.
            var left = item.ExpiresAt!.Value - DateTime.UtcNow;
            var text = left.TotalDays >= 1 ? $"{(int)left.TotalDays} días" : $"{Math.Max(1, (int)left.TotalHours)} horas";
            await player.ShowBlueMessageAsync($"{item.Definition?.GetNameForLevel(item.Level)}: vence en {text}.").ConfigureAwait(false);
        }
    }

    private static async ValueTask RemoveExpiredItemsAsync(Player player)
    {
        if (player.Inventory is not { } inventory)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var item in inventory.Items.Where(i => i.ExpiresAt <= now).ToList())
        {
            var slot = item.ItemSlot;
            await inventory.RemoveItemAsync(item).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
            await player.ShowBlueMessageAsync($"{item.Definition?.GetNameForLevel(item.Level)}: se terminó el tiempo y desapareció.").ConfigureAwait(false);
        }
    }

    private static void AddWings(Character character, ItemStorage inventory, GkzWelcomeKitConfiguration configuration, IContext persistenceContext, GameConfiguration gameConfiguration, ILogger logger)
    {
        // The first wings of each class which can be created; the Dark Lord uses its cape.
        (byte Group, short Number)? wings = character.CharacterClass?.Number switch
        {
            0 => (12, 1), // Dark Wizard: Wings of Heaven
            4 or 12 => (12, 2), // Dark Knight and Magic Gladiator: Wings of Satan
            8 => (12, 0), // Fairy Elf: Wings of Elf
            16 => (13, 30), // Dark Lord: Cape of Lord
            _ => null,
        };
        if (wings is not { } identifier
            || gameConfiguration.Items.FirstOrDefault(d => d.Group == identifier.Group && d.Number == identifier.Number) is not { } definition)
        {
            logger.LogWarning("No hay alas de bienvenida para la clase {class}.", character.CharacterClass?.Number);
            return;
        }

        if (FindFreeSlot(inventory, definition) is not { } slot)
        {
            logger.LogWarning("No hay lugar para las alas de bienvenida de {character}.", character.Name);
            return;
        }

        var item = persistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = definition.Durability;
        item.ItemSlot = slot;
        item.ExpiresAt = DateTime.UtcNow.AddDays(configuration.WingsDays);
        inventory.Items.Add(item);
    }

    private static byte? FindFreeSlot(ItemStorage inventory, ItemDefinition definition)
    {
        var firstSlot = InventoryConstants.EquippableSlotsCount;
        var occupied = new bool[InventoryConstants.InventoryRows, InventoryConstants.RowSize];
        foreach (var item in inventory.Items.Where(i => i.ItemSlot >= firstSlot && i.Definition is not null))
        {
            var row = (item.ItemSlot - firstSlot) / InventoryConstants.RowSize;
            var column = (item.ItemSlot - firstSlot) % InventoryConstants.RowSize;
            for (var r = row; r < row + item.Definition!.Height && r < InventoryConstants.InventoryRows; r++)
            {
                for (var c = column; c < column + item.Definition.Width && c < InventoryConstants.RowSize; c++)
                {
                    occupied[r, c] = true;
                }
            }
        }

        for (var row = 0; row <= InventoryConstants.InventoryRows - definition.Height; row++)
        {
            for (var column = 0; column <= InventoryConstants.RowSize - definition.Width; column++)
            {
                var isFree = true;
                for (var r = row; isFree && r < row + definition.Height; r++)
                {
                    for (var c = column; isFree && c < column + definition.Width; c++)
                    {
                        isFree = !occupied[r, c];
                    }
                }

                if (isFree)
                {
                    return (byte)(firstSlot + (row * InventoryConstants.RowSize) + column);
                }
            }
        }

        return null;
    }
}
