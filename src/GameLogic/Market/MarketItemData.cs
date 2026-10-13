// <copyright file="MarketItemData.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;

/// <summary>
/// MU GKZ: an item for sale in the market, as data. The market keeps the items as this copy instead of item
/// entities, because the loader of the persistence doesn't load items with all their options inside another
/// entity. The copy refers to the item definition by group and number, and to options and sets by their ids.
/// </summary>
public static class MarketItemData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Writes the item as data.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The data.</returns>
    public static string Serialize(Item item)
    {
        var data = new Data(
            item.Definition!.Group,
            item.Definition.Number,
            item.Level,
            item.Durability,
            item.HasSkill,
            item.SocketCount,
            item.PetExperience,
            item.ItemOptions.Where(o => o.ItemOption is not null).Select(o => new OptionData(o.ItemOption!.GetId(), o.Level, o.Index)).ToList(),
            item.ItemSetGroups.Select(s => s.GetId()).ToList());
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    /// <summary>
    /// Creates the item of the data, as a temporary item which isn't persisted.
    /// </summary>
    /// <param name="json">The data.</param>
    /// <param name="configuration">The game configuration, which has the definitions, options and sets.</param>
    /// <returns>The item; <c>null</c>, if the data is invalid or its item definition doesn't exist any more.</returns>
    public static TemporaryItem? Deserialize(string json, GameConfiguration configuration)
    {
        Data? data;
        try
        {
            data = JsonSerializer.Deserialize<Data>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (data is null || configuration.Items.FirstOrDefault(d => d.Group == data.Group && d.Number == data.Number) is not { } definition)
        {
            return null;
        }

        var item = new TemporaryItem
        {
            Definition = definition,
            Level = data.Level,
            Durability = data.Durability,
            HasSkill = data.HasSkill,
            SocketCount = data.SocketCount,
            PetExperience = data.PetExperience,
        };

        var options = GetOptions(configuration, definition);
        foreach (var option in data.Options)
        {
            if (options.GetValueOrDefault(option.Option) is { } itemOption)
            {
                item.ItemOptions.Add(new ItemOptionLink { ItemOption = itemOption, Level = option.Level, Index = option.Index });
            }
        }

        foreach (var setId in data.ItemSets)
        {
            if ((configuration.ItemSetGroups ?? []).SelectMany(g => g.Items).FirstOrDefault(s => s.GetId() == setId) is { } itemOfSet)
            {
                item.ItemSetGroups.Add(itemOfSet);
            }
        }

        return item;
    }

    private static Dictionary<Guid, IncreasableItemOption> GetOptions(GameConfiguration configuration, ItemDefinition definition)
    {
        var options = new Dictionary<Guid, IncreasableItemOption>();
        var sets = configuration.ItemSetGroups ?? [];
        var sources = (definition.PossibleItemOptions ?? [])
            .Concat(configuration.ItemOptions ?? [])
            .SelectMany(d => d.PossibleOptions)
            .Concat(sets.Where(g => g.Options is not null).SelectMany(g => g.Options!.PossibleOptions))
            .Concat(sets.SelectMany(g => g.Items).Where(i => i.BonusOption is not null).Select(i => i.BonusOption!));
        foreach (var option in sources)
        {
            options.TryAdd(option.GetId(), option);
        }

        return options;
    }

    private sealed record OptionData(Guid Option, int Level, int Index);

    private sealed record Data(byte Group, short Number, byte Level, double Durability, bool HasSkill, int SocketCount, int PetExperience, List<OptionData> Options, List<Guid> ItemSets);
}
