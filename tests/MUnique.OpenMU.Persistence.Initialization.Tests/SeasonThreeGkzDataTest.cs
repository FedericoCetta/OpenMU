// <copyright file="SeasonThreeGkzDataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonThreeGkz;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the MU GKZ Season 3 data initialization.
/// </summary>
[TestFixture]
internal class SeasonThreeGkzDataTest
{
    private InMemoryPersistenceContextProvider _contextProvider = null!;
    private GameConfiguration _configuration = null!;

    /// <summary>
    /// Initializes the data once for all tests of this fixture.
    /// </summary>
    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        this._contextProvider = new InMemoryPersistenceContextProvider();
        IDataInitializationPlugIn initialization = new DataInitialization(this._contextProvider, NullLoggerFactory.Instance);
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = this._contextProvider.CreateNewContext();
        this._configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
    }

    /// <summary>
    /// Tests that only the classic character classes and their evolutions exist.
    /// </summary>
    [Test]
    public void OnlyClassicCharacterClassesExist()
    {
        var classNumbers = this._configuration.CharacterClasses.Select(c => (int)c.Number).Order();
        Assert.That(classNumbers, Is.EqualTo(new[] { 0, 2, 3, 4, 6, 7, 8, 10, 11, 12, 13, 16, 17 }));
    }

    /// <summary>
    /// Tests that the maximum level stays 400 and the master level system is gone.
    /// </summary>
    [Test]
    public void NoMasterLevel()
    {
        Assert.That(this._configuration.MaximumLevel, Is.EqualTo(400));
        Assert.That(this._configuration.MaximumMasterLevel, Is.Zero);
        Assert.That(this._configuration.MasterSkillRoots, Is.Empty);
        Assert.That(this._configuration.Skills.Where(s => s.MasterDefinition is not null), Is.Empty);
    }

    /// <summary>
    /// Tests that the removed maps don't exist, but the kept ones, including Kanturu and the maps of the third class quest.
    /// </summary>
    [Test]
    public void MapsOfSeasonThree()
    {
        var mapNumbers = this._configuration.Maps.Select(m => m.Number).ToHashSet();
        Assert.That(mapNumbers.Intersect(SeasonThreeScope.RemovedMaps), Is.Empty);
        short[] keptMaps = [0, 1, 2, 3, 4, 7, 8, 10, 30, 31, 33, 34, 37, 38, 39, 41, 42];
        Assert.That(mapNumbers, Is.SupersetOf(keptMaps));
    }

    /// <summary>
    /// Tests that the first, second and third wings exist, but not the wings of removed classes and newer ones.
    /// </summary>
    [Test]
    public void WingsOfSeasonThree()
    {
        var items = this._configuration.Items.Select(i => (i.Group, i.Number)).ToHashSet();
        (byte, short)[] keptWings = [(12, 0), (12, 1), (12, 2), (12, 3), (12, 4), (12, 5), (12, 6), (12, 36), (12, 37), (12, 38), (12, 39), (12, 40), (13, 30)];
        (byte, short)[] removedWings = [(12, 41), (12, 42), (12, 43), (12, 49), (12, 50), (12, 130), (12, 131), (12, 132), (12, 133), (12, 134), (12, 135)];
        Assert.That(items, Is.SupersetOf(keptWings));
        Assert.That(items.Intersect(removedWings), Is.Empty);
    }

    /// <summary>
    /// Tests that the socket system is gone.
    /// </summary>
    [Test]
    public void NoSocketSystem()
    {
        Assert.That(this._configuration.Items.Where(i => i.MaximumSockets > 0), Is.Empty);
        Assert.That(this._configuration.Items.Where(i => i.Group == 12 && i.Number is >= 60 and <= 129), Is.Empty);
        Assert.That(this._configuration.ItemOptions.SelectMany(o => o.PossibleOptions).Where(o => ItemOptionTypes.SocketOption.Equals(o.OptionType)), Is.Empty);
        Assert.That(this._configuration.ItemOptionCombinationBonuses, Is.Empty);
    }

    /// <summary>
    /// Tests that no item or skill is only usable by a removed class.
    /// </summary>
    [Test]
    public void NoItemsOrSkillsOfRemovedClasses()
    {
        Assert.That(this._configuration.Items.Where(i => i.QualifiedCharacters.Any(c => SeasonThreeScope.RemovedCharacterClasses.Contains(c.Number))), Is.Empty);
        Assert.That(this._configuration.Skills.Where(s => s.QualifiedCharacters.Any(c => SeasonThreeScope.RemovedCharacterClasses.Contains(c.Number))), Is.Empty);
    }

    /// <summary>
    /// Tests that only the legacy quests remain, including the third class quests.
    /// </summary>
    [Test]
    public void OnlyLegacyQuests()
    {
        var quests = this._configuration.Monsters.SelectMany(m => m.Quests).ToList();
        Assert.That(quests.Where(q => q.Group != SeasonThreeScope.LegacyQuestGroup), Is.Empty);
        Assert.That(quests.Where(q => q.QualifiedCharacter is { } c && SeasonThreeScope.RemovedCharacterClasses.Contains(c.Number)), Is.Empty);
        Assert.That(quests.Select(q => q.QuestGiver?.Number), Does.Contain((short)406), "Apostle Devin of the third class quest is missing");
    }

    /// <summary>
    /// Tests that the classic mini games exist, but not the newer ones.
    /// </summary>
    [Test]
    public void MiniGamesOfSeasonThree()
    {
        var types = this._configuration.MiniGameDefinitions.Select(g => g.Type).ToHashSet();
        Assert.That(types, Does.Contain(MiniGameType.BloodCastle).And.Contain(MiniGameType.DevilSquare).And.Contain(MiniGameType.ChaosCastle));
        Assert.That(types, Does.Not.Contain(MiniGameType.Doppelganger).And.Not.Contain(MiniGameType.ImperialGuardian));
    }

    /// <summary>
    /// Tests that the plugins of removed features are deactivated.
    /// </summary>
    [Test]
    public void PlugInsOfRemovedFeaturesAreInactive()
    {
        var deactivated = SeasonThreeScope.DeactivatedPlugIns.Select(t => t.GUID).ToHashSet();
        var configurations = this._configuration.PlugInConfigurations.Where(c => deactivated.Contains(c.TypeId)).ToList();
        Assert.That(configurations, Has.Count.EqualTo(deactivated.Count));
        Assert.That(configurations.Where(c => c.IsActive), Is.Empty);
    }

    /// <summary>
    /// Tests that at most 2 accounts per IP address can be logged in, on all game servers together.
    /// </summary>
    [Test]
    public void ConnectionsPerIpAreLimited()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugIn).GUID;
        var plugInConfiguration = this._configuration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        var configuration = plugInConfiguration.GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugInConfiguration>(null);

        Assert.That(plugInConfiguration.IsActive, Is.True);
        Assert.That(configuration!.MaximumConnectionsPerIp, Is.EqualTo(2));
        Assert.That(configuration.CountAllGameServers, Is.True);
    }

    /// <summary>
    /// Tests that the jewels stack up to 50 pieces, and that they're still not wearable.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    [TestCase(14, 13)]
    [TestCase(14, 14)]
    [TestCase(14, 16)]
    [TestCase(12, 15)]
    [TestCase(14, 22)]
    public void JewelsStackUpToFifty(byte group, short number)
    {
        var jewel = this._configuration.Items.Single(i => i.Group == group && i.Number == number);

        Assert.That(jewel.Durability, Is.EqualTo(50));
        Assert.That(jewel.ItemSlot, Is.Null);
    }

    /// <summary>
    /// Tests that the potions stack up to 255 pieces, and that the siege potion keeps its own stack.
    /// </summary>
    /// <param name="number">The item number in group 14.</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(6)]
    [TestCase(8)]
    [TestCase(35)]
    [TestCase(40)]
    public void PotionsStackUpTo255(short number)
    {
        var potion = this._configuration.Items.Single(i => i.Group == 14 && i.Number == number);
        var siegePotion = this._configuration.Items.Single(i => i.Group == 14 && i.Number == 7);

        Assert.That(potion.Durability, Is.EqualTo(255));
        Assert.That(potion.ItemSlot, Is.Null);
        Assert.That(siegePotion.Durability, Is.Not.EqualTo(255));
    }

    /// <summary>
    /// Tests that the equipped items lose their durability three times slower than in the original game.
    /// </summary>
    [Test]
    public void ItemsLastThreeTimesLonger()
    {
        Assert.That(this._configuration.DamagePerOneItemDurability, Is.EqualTo(69 * 3));
        Assert.That(this._configuration.DamagePerOnePetDurability, Is.EqualTo(100 * 3));
        Assert.That(this._configuration.HitsPerOneItemDurability, Is.EqualTo(564 * 3));
    }

    /// <summary>
    /// Tests that the Talisman of Chaos Assembly exists, and that only the upgrades to +11, +12 and +13 accept it.
    /// All level upgrades use the MU GKZ handler, which refuses the talisman in the others.
    /// </summary>
    [Test]
    public void ChaosAssemblyTalismanProtectsElevenToThirteen()
    {
        var talisman = this._configuration.Items.Single(i => i.Group == 14 && i.Number == 96);
        var upgrades = this._configuration.Monsters
            .Single(m => m.NpcWindow == NpcWindow.ChaosMachine)
            .ItemCraftings
            .Select(c => (Crafting: c, Level: GkzChaosAssemblyTalisman.GetUpgradeTargetLevel(c)))
            .Where(u => u.Level is not null)
            .ToList();

        Assert.That(upgrades.Select(u => u.Level), Is.EquivalentTo(new int?[] { 10, 11, 12, 13, 14, 15 }));
        foreach (var (crafting, level) in upgrades)
        {
            Assert.That(crafting.ItemCraftingHandlerClassName, Is.EqualTo(typeof(MUnique.OpenMU.GameLogic.PlayerActions.Craftings.GkzItemUpgradeCrafting).FullName));
            var acceptsTalisman = crafting.SimpleCraftingSettings!.RequiredItems.Any(r => r.PossibleItems.Contains(talisman) && r.MinimumAmount == 0 && r.MaximumAmount == 1);
            Assert.That(acceptsTalisman, Is.EqualTo(level is >= 11 and <= 13), $"+{level}");
        }
    }

    /// <summary>
    /// Tests that the item shop of the X key exists with the catalog of GkzCashShop.json: everything for sale,
    /// paid with GKZ Coins, deliverable, and nothing else of the Season 6 shop.
    /// </summary>
    [Test]
    public void ItemShopHasTheGkzCatalog()
    {
        var catalog = GkzCashShop.Load();
        var shop = this._configuration.CashShopConfiguration;

        Assert.That(shop, Is.Not.Null);
        Assert.That((shop!.ScriptSaleZone, shop.ScriptYear, shop.ScriptYearId), Is.EqualTo((catalog.ScriptVersion.Zone, catalog.ScriptVersion.Year, catalog.ScriptVersion.YearId)));
        Assert.That(catalog.Packages.Select(p => p.Seq), Is.Unique);
        Assert.That(shop.Packages.Select(p => p.PackageSequence), Is.EquivalentTo(catalog.Packages.Select(p => p.Seq)));
        foreach (var package in shop.Packages)
        {
            var entry = catalog.Packages.Single(p => p.Seq == package.PackageSequence);
            var product = package.Products.Single();
            Assert.That(package.IsForSale, Is.True, package.Name);
            Assert.That(package.CoinType, Is.EqualTo(CashShopCoinType.WCoinC), package.Name);
            Assert.That(package.Price, Is.EqualTo(entry.Price), package.Name);
            Assert.That((product.ItemDefinition!.Group, product.ItemDefinition.Number, product.ItemLevel), Is.EqualTo((entry.Group, entry.Number, entry.Level)), package.Name);
        }
    }

    /// <summary>
    /// Tests that the database is marked with the key of this initialization, so that no Season 6 updates are offered.
    /// </summary>
    [Test]
    public async Task NoSeasonSixUpdatesAreOfferedAsync()
    {
        using (var context = this._contextProvider.CreateNewContext())
        {
            var state = (await context.GetAsync<ConfigurationUpdateState>().ConfigureAwait(false)).Single();
            Assert.That(state.InitializationKey, Is.EqualTo(DataInitialization.Id));
        }

        var plugInManager = new PlugInManager(null, NullLoggerFactory.Instance, null, null);
        plugInManager.DiscoverAndRegisterPlugIns();
        var updates = await new DataUpdateService(this._contextProvider, plugInManager).DetermineAvailableUpdatesAsync().ConfigureAwait(false);
        Assert.That(updates, Is.Empty);
    }

    /// <summary>
    /// Tests that every configuration object which is reachable from the game configuration is part of it,
    /// i.e. that no reference to removed content is left.
    /// </summary>
    [Test]
    public void NoDanglingReferences()
    {
        var owned = new Dictionary<Type, HashSet<object>>
        {
            { typeof(ItemDefinition), this._configuration.Items.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(CharacterClass), this._configuration.CharacterClasses.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(GameMapDefinition), this._configuration.Maps.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(Skill), this._configuration.Skills.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(MonsterDefinition), this._configuration.Monsters.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(DropItemGroup), this._configuration.DropItemGroups.Concat<object>(this._configuration.Monsters.SelectMany(m => m.Quests).SelectMany(q => q.RequiredItems).Select(r => r.DropItemGroup).OfType<DropItemGroup>()).Concat(this._configuration.Items.SelectMany(i => i.DropItems)).ToHashSet(ReferenceEqualityComparer.Instance) },
            { typeof(ItemSetGroup), this._configuration.ItemSetGroups.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(ItemOptionDefinition), this._configuration.ItemOptions.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(MiniGameDefinition), this._configuration.MiniGameDefinitions.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(MasterSkillRoot), this._configuration.MasterSkillRoots.ToHashSet<object>(ReferenceEqualityComparer.Instance) },
            { typeof(ExitGate), this._configuration.Maps.SelectMany(m => m.ExitGates).ToHashSet<object>(ReferenceEqualityComparer.Instance) },
        };

        var dangling = new List<string>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(object Value, string Path)>();
        pending.Push((this._configuration, nameof(GameConfiguration)));
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current.Value))
            {
                continue;
            }

            foreach (var property in GetModelProperties(current.Value.GetType()))
            {
                var value = property.GetValue(current.Value);
                var values = value is IEnumerable enumerable and not string ? enumerable.Cast<object>() : value is null ? [] : [value];
                foreach (var child in values.Where(v => GetModelType(v.GetType()) is not null))
                {
                    var path = $"{current.Path}.{property.Name}";
                    var ownedKey = owned.Keys.FirstOrDefault(k => k.IsInstanceOfType(child));
                    if (ownedKey is not null && !owned[ownedKey].Contains(child))
                    {
                        dangling.Add($"{path} -> {child}");
                        continue;
                    }

                    pending.Push((child, path));
                }
            }
        }

        Assert.That(dangling, Is.Empty);
    }

    private static Type? GetModelType(Type type)
    {
        Assembly[] modelAssemblies = [typeof(GameConfiguration).Assembly, typeof(AttributeDefinition).Assembly];
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (modelAssemblies.Contains(current.Assembly))
            {
                return current;
            }
        }

        return null;
    }

    private static IEnumerable<PropertyInfo> GetModelProperties(Type type)
    {
        return GetModelType(type)?.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && !p.PropertyType.IsValueType && p.PropertyType != typeof(string))
               ?? [];
    }
}
