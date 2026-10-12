// <copyright file="SeasonThreeContentPruner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic;

/// <summary>
/// Removes the content which isn't part of Season 3 from a freshly created Season 6 game configuration.
/// </summary>
/// <remarks>
/// It has to run before the configuration is saved for the first time. It works in three steps:
/// <list type="number">
///   <item>The removed content is selected by <see cref="SeasonThreeScope"/>, including the objects it owns.</item>
///   <item>The object graph is walked from the game configuration, and every reference to removed content is cut.
///         Depending on the reference, the referencing object is removed as well (e.g. a gate which leads to a
///         removed map), or the reference is just cleared. This repeats until nothing changes anymore.</item>
///   <item>The removed objects are detached from the context, so they are not saved.</item>
/// </list>
/// The graph walk is generic on purpose, so that new content of the Season 6 data can't leave dangling references.
/// </remarks>
internal class SeasonThreeContentPruner : InitializerBase
{
    private static readonly ConcurrentDictionary<Type, ModelProperty[]> PropertiesByType = new();

    /// <summary>
    /// References without which the referencing object makes no sense, so it's removed together with the referenced object.
    /// </summary>
    private static readonly HashSet<(Type Owner, string Property)> RequiredReferences =
    [
        (typeof(ExitGate), nameof(ExitGate.Map)),
        (typeof(EnterGate), nameof(EnterGate.TargetGate)),
        (typeof(WarpInfo), nameof(WarpInfo.Gate)),
        (typeof(MiniGameDefinition), nameof(MiniGameDefinition.Entrance)),
        (typeof(MiniGameDefinition), nameof(MiniGameDefinition.TicketItem)),
        (typeof(MonsterSpawnArea), nameof(MonsterSpawnArea.MonsterDefinition)),
        (typeof(MonsterSpawnArea), nameof(MonsterSpawnArea.GameMap)),
        (typeof(ItemOfItemSet), nameof(ItemOfItemSet.ItemDefinition)),
        (typeof(QuestDefinition), nameof(QuestDefinition.QualifiedCharacter)),
        (typeof(QuestDefinition), nameof(QuestDefinition.QuestGiver)),
        (typeof(QuestItemRequirement), nameof(QuestItemRequirement.Item)),
        (typeof(QuestReward), nameof(QuestReward.ItemReward)),
        (typeof(JewelMix), nameof(JewelMix.SingleJewel)),
        (typeof(JewelMix), nameof(JewelMix.MixedJewel)),
        (typeof(ItemCrafting), nameof(ItemCrafting.SimpleCraftingSettings)),
        (typeof(ItemCraftingResultItem), nameof(ItemCraftingResultItem.ItemDefinition)),
        (typeof(CashShopProduct), nameof(CashShopProduct.ItemDefinition)),
        (typeof(Item), nameof(Item.Definition)),
        (typeof(ItemOptionLink), nameof(ItemOptionLink.ItemOption)),
    ];

    /// <summary>
    /// Collections whose owner is removed when all of its elements got removed.
    /// An empty list of qualified characters, for example, would make an item usable by every class.
    /// </summary>
    private static readonly HashSet<(Type Owner, string Property)> OwnerRemovedWhenEmptied =
    [
        (typeof(ItemDefinition), nameof(ItemDefinition.QualifiedCharacters)),
        (typeof(Skill), nameof(Skill.QualifiedCharacters)),
        (typeof(DropItemGroup), nameof(DropItemGroup.PossibleItems)),
        (typeof(ItemSetGroup), nameof(ItemSetGroup.Items)),
        (typeof(ItemCraftingRequiredItem), nameof(ItemCraftingRequiredItem.PossibleItems)),
        (typeof(SimpleCraftingSettings), nameof(SimpleCraftingSettings.ResultItems)),
        (typeof(CashShopPackage), nameof(CashShopPackage.Products)),
    ];

    /// <summary>
    /// Collections whose owner is removed as soon as one of its elements got removed.
    /// Without one of the required items, a crafting or quest would get cheaper than intended.
    /// </summary>
    private static readonly HashSet<(Type Owner, string Property)> OwnerRemovedOnAnyRemoval =
    [
        (typeof(SimpleCraftingSettings), nameof(SimpleCraftingSettings.RequiredItems)),
        (typeof(QuestDefinition), nameof(QuestDefinition.RequiredItems)),
    ];

    private static readonly Assembly[] ModelAssemblies = [typeof(GameConfiguration).Assembly, typeof(AttributeDefinition).Assembly];

    private readonly HashSet<object> _removed = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonThreeContentPruner"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public SeasonThreeContentPruner(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.SelectRemovedContent();
        this.RemoveReferencesToRemovedContent();
        this.DetachRemovedContent();
    }

    private static bool IsOnlyForRemovedClasses(ICollection<CharacterClass> qualifiedCharacters)
    {
        return qualifiedCharacters.Count > 0
               && qualifiedCharacters.All(c => SeasonThreeScope.RemovedCharacterClasses.Contains(c.Number));
    }

    private static Type? GetModelType(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (ModelAssemblies.Contains(current.Assembly))
            {
                return current;
            }
        }

        return null;
    }

    private static bool IsModelObject(object value) => GetModelProperties(value.GetType()).Length > 0;

    private static ModelProperty[] GetModelProperties(Type type)
    {
        return PropertiesByType.GetOrAdd(type, static t =>
        {
            if (GetModelType(t) is not { } modelType)
            {
                return [];
            }

            return modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Where(p => !p.PropertyType.IsValueType && p.PropertyType != typeof(string) && p.PropertyType != typeof(byte[]))
                .Select(p => new ModelProperty(
                    p,
                    (p.DeclaringType!, p.Name),
                    p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>),
                    p.GetCustomAttribute<MemberOfAggregateAttribute>() is not null))
                .ToArray();
        });
    }

    private void SelectRemovedContent()
    {
        var configuration = this.GameConfiguration;

        configuration.CharacterClasses.Where(c => SeasonThreeScope.RemovedCharacterClasses.Contains(c.Number)).ForEach(this.MarkRemoved);
        configuration.Maps.Where(m => SeasonThreeScope.RemovedMaps.Contains(m.Number)).ForEach(this.MarkRemoved);
        configuration.Maps.SelectMany(m => m.MonsterSpawns)
            .Where(s => s.MonsterDefinition is { } monster && SeasonThreeScope.RemovedNpcSpawns.Contains(monster.Number))
            .ForEach(this.MarkRemoved);

        configuration.Items
            .Where(i => SeasonThreeScope.RemovedItems.Contains((i.Group, i.Number))
                        || i.MaximumSockets > 0
                        || IsOnlyForRemovedClasses(i.QualifiedCharacters))
            .ForEach(this.MarkRemoved);

        // No master level in Season 3, so the whole master skill tree goes away.
        configuration.Skills
            .Where(s => s.MasterDefinition is not null || IsOnlyForRemovedClasses(s.QualifiedCharacters))
            .ForEach(this.MarkRemoved);
        configuration.MasterSkillRoots.ForEach(this.MarkRemoved);

        // The quests of the Elf Soldier, which came with Season 4.
        configuration.Monsters.SelectMany(m => m.Quests)
            .Where(q => q.Group != SeasonThreeScope.LegacyQuestGroup)
            .ForEach(this.MarkRemoved);

        configuration.MiniGameDefinitions
            .Where(g => g.Type is MiniGameType.Doppelganger or MiniGameType.ImperialGuardian)
            .ForEach(this.MarkRemoved);

        // Socket options and their combination bonuses.
        configuration.ItemOptions
            .Where(o => o.PossibleOptions.Any(p => ItemOptionTypes.SocketOption.Equals(p.OptionType)))
            .ForEach(this.MarkRemoved);
        configuration.ItemOptionCombinationBonuses.ForEach(this.MarkRemoved);

        if (configuration.CashShopConfiguration is { } cashShop)
        {
            this.MarkRemoved(cashShop);
        }
    }

    private void MarkRemoved(object obj)
    {
        if (!this._removed.Add(obj))
        {
            return;
        }

        // Everything which is owned by the removed object is removed, too.
        foreach (var property in GetModelProperties(obj.GetType()).Where(p => p.IsAggregate))
        {
            switch (property.Info.GetValue(obj))
            {
                case null:
                    break;
                case IEnumerable children:
                    children.Cast<object>().Where(IsModelObject).ToList().ForEach(this.MarkRemoved);
                    break;
                case var child when IsModelObject(child):
                    this.MarkRemoved(child);
                    break;
            }
        }
    }

    private void RemoveReferencesToRemovedContent()
    {
        bool changed;
        do
        {
            changed = false;
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<object>();
            pending.Push(this.GameConfiguration);
            while (pending.TryPop(out var owner))
            {
                if (this._removed.Contains(owner) || !visited.Add(owner))
                {
                    continue;
                }

                foreach (var property in GetModelProperties(owner.GetType()))
                {
                    changed |= property.IsCollection
                        ? this.RemoveFromCollection(owner, property, pending)
                        : this.RemoveReference(owner, property, pending);
                }
            }
        }
        while (changed);
    }

    private bool RemoveFromCollection(object owner, ModelProperty property, Stack<object> pending)
    {
        if (property.Info.GetValue(owner) is not IEnumerable collection)
        {
            return false;
        }

        var elements = collection.Cast<object>().ToList();
        var removedElements = elements.Where(this._removed.Contains).ToList();
        elements.Where(e => !this._removed.Contains(e) && IsModelObject(e)).ForEach(pending.Push);
        if (removedElements.Count == 0)
        {
            return false;
        }

        var removeMethod = collection.GetType().GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
            .GetMethod(nameof(ICollection<object>.Remove))!;
        removedElements.ForEach(e => removeMethod.Invoke(collection, [e]));

        if (OwnerRemovedOnAnyRemoval.Contains(property.Key)
            || (OwnerRemovedWhenEmptied.Contains(property.Key) && removedElements.Count == elements.Count))
        {
            this.MarkRemoved(owner);
        }

        return true;
    }

    private bool RemoveReference(object owner, ModelProperty property, Stack<object> pending)
    {
        var value = property.Info.GetValue(owner);
        if (value is null)
        {
            return false;
        }

        if (!this._removed.Contains(value))
        {
            if (IsModelObject(value))
            {
                pending.Push(value);
            }

            return false;
        }

        if (RequiredReferences.Contains(property.Key) || !property.Info.CanWrite)
        {
            this.MarkRemoved(owner);
        }
        else
        {
            property.Info.SetValue(owner, null);
        }

        return true;
    }

    private void DetachRemovedContent()
    {
        // Objects which are only reachable through removed content (e.g. the item reward of a removed quest)
        // would still be saved, and would still reference removed content. So they are detached as well.
        var kept = this.CollectReachableObjects([this.GameConfiguration]);
        var orphaned = this.CollectReachableObjects(this._removed).Where(o => !kept.Contains(o));

        foreach (var obj in this._removed.Concat(orphaned).ToList())
        {
            this.Context.Detach(obj);
        }
    }

    private HashSet<object> CollectReachableObjects(IEnumerable<object> roots)
    {
        var reachable = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<object>(roots);
        var isFromRemoved = !roots.Contains(this.GameConfiguration);
        while (pending.TryPop(out var current))
        {
            if (!reachable.Add(current))
            {
                continue;
            }

            foreach (var property in GetModelProperties(current.GetType()))
            {
                var value = property.Info.GetValue(current);
                var values = property.IsCollection && value is IEnumerable collection ? collection.Cast<object>() : value is null ? [] : [value];
                foreach (var child in values.Where(IsModelObject))
                {
                    // The kept graph never enters removed content; the removed graph never enters the game configuration.
                    if ((!isFromRemoved && this._removed.Contains(child)) || (isFromRemoved && child is GameConfiguration))
                    {
                        continue;
                    }

                    pending.Push(child);
                }
            }
        }

        return reachable;
    }

    private sealed record ModelProperty(PropertyInfo Info, (Type Owner, string Property) Key, bool IsCollection, bool IsAggregate);
}
