// <copyright file="GkzWelcomeKitPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Buffers.Binary;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.GameLogic.WelcomeKit;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests for the <see cref="GkzWelcomeKitPlugIn"/>.
/// </summary>
[TestFixture]
public class GkzWelcomeKitPlugInTest
{
    /// <summary>
    /// A new character gets the points, the zen and the wings of its class, which expire.
    /// </summary>
    /// <param name="classNumber">The number of the character class.</param>
    /// <param name="group">The item group of the expected wings.</param>
    /// <param name="number">The item number of the expected wings.</param>
    [TestCase(0, 12, 1)]
    [TestCase(4, 12, 2)]
    [TestCase(8, 12, 0)]
    [TestCase(12, 12, 2)]
    [TestCase(16, 13, 30)]
    public void NewCharacterGetsTheKit(int classNumber, int group, int number)
    {
        var plugIn = new GkzWelcomeKitPlugIn { Configuration = new GkzWelcomeKitConfiguration { EndUtc = DateTime.UtcNow.AddDays(1) } };
        var account = new Account();
        var character = CreateCharacter(classNumber);

        Create(plugIn, account, character);

        Assert.That(account.WelcomeKitsClaimed, Is.EqualTo(1));
        Assert.That(character.LevelUpPoints, Is.EqualTo(5000));
        Assert.That(character.Inventory!.Money, Is.EqualTo(5_000_000));
        var wings = character.Inventory.Items.Single();
        Assert.That(wings.Definition!.Group, Is.EqualTo(group));
        Assert.That(wings.Definition.Number, Is.EqualTo(number));
        Assert.That(wings.ItemSlot, Is.GreaterThanOrEqualTo(InventoryConstants.EquippableSlotsCount));
        Assert.That(wings.ExpiresAt, Is.EqualTo(DateTime.UtcNow.AddDays(4)).Within(TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// After the end of the period, new characters don't get the kit.
    /// </summary>
    [Test]
    public void NoKitAfterTheEnd()
    {
        var plugIn = new GkzWelcomeKitPlugIn { Configuration = new GkzWelcomeKitConfiguration { EndUtc = DateTime.UtcNow.AddDays(-1) } };
        var account = new Account();
        var character = CreateCharacter(0);

        Create(plugIn, account, character);

        Assert.That(account.WelcomeKitsClaimed, Is.Zero);
        Assert.That(character.LevelUpPoints, Is.Zero);
        Assert.That(character.Inventory!.Money, Is.Zero);
        Assert.That(character.Inventory.Items, Is.Empty);
    }

    /// <summary>
    /// An account gets five kits at most, also when its characters are deleted in between.
    /// </summary>
    [Test]
    public void AccountGetsFiveKitsAtMost()
    {
        var plugIn = new GkzWelcomeKitPlugIn { Configuration = new GkzWelcomeKitConfiguration { EndUtc = DateTime.UtcNow.AddDays(1) } };
        var account = new Account();
        for (var i = 0; i < 5; i++)
        {
            Create(plugIn, account, CreateCharacter(0));
        }

        var sixth = CreateCharacter(0);
        Create(plugIn, account, sixth);

        Assert.That(account.WelcomeKitsClaimed, Is.EqualTo(5));
        Assert.That(sixth.Inventory!.Money, Is.Zero);
        Assert.That(sixth.Inventory.Items, Is.Empty);
    }

    /// <summary>
    /// The wings don't land on an item which the character already has.
    /// </summary>
    [Test]
    public void WingsDoNotOverlapExistingItems()
    {
        var plugIn = new GkzWelcomeKitPlugIn { Configuration = new GkzWelcomeKitConfiguration { EndUtc = DateTime.UtcNow.AddDays(1) } };
        var character = CreateCharacter(0);
        var firstSlot = InventoryConstants.EquippableSlotsCount;
        character.Inventory!.Items.Add(new Item { ItemSlot = firstSlot, Definition = new ItemDefinition { Width = 1, Height = 1 } });

        Create(plugIn, new Account(), character);

        var wings = character.Inventory.Items.Single(i => i.ExpiresAt is not null);
        Assert.That(wings.ItemSlot, Is.EqualTo(firstSlot + 1));
    }

    /// <summary>
    /// The client gets the expiration time with the item data, marked by the highest bit of the level.
    /// </summary>
    [Test]
    public void ExpirationIsSerializedWithTheItem()
    {
        var expiresAt = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        var item = CreateWings(expiresAt);
        var data = new byte[new ItemSerializerExtended().NeededSpace];

        var length = new ItemSerializerExtended().SerializeItem(data, item);

        Assert.That(length, Is.EqualTo(9));
        Assert.That(data[2], Is.EqualTo(0x83));
        Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(5)), Is.EqualTo((uint)new DateTimeOffset(expiresAt).ToUnixTimeSeconds()));
    }

    /// <summary>
    /// Other items keep their format, also when the buffer contains old data.
    /// </summary>
    [Test]
    public void ItemWithoutExpirationKeepsItsFormat()
    {
        var item = CreateWings(null);
        var data = new byte[new ItemSerializerExtended().NeededSpace];
        data.AsSpan().Fill(0xFF);

        var length = new ItemSerializerExtended().SerializeItem(data, item);

        Assert.That(length, Is.EqualTo(5));
        Assert.That(data[2], Is.EqualTo(3));
    }

    /// <summary>
    /// A piece of the set of the reward of 501 resets is +13 +28 with luck, the option of level 380 and exactly three excellent options.
    /// </summary>
    [Test]
    public void ResetSetPieceHasThreeExcellentOptions()
    {
        var definition = new Mock<ItemOptionDefinition> { CallBase = true };
        var normal = new Mock<IncreasableItemOption> { CallBase = true };
        normal.Setup(o => o.LevelDependentOptions).Returns(new List<ItemOptionOfLevel> { new() { Level = 4 }, new() { Level = 7 } });
        normal.Object.OptionType = ItemOptionTypes.Option;
        var options = new List<IncreasableItemOption> { normal.Object, new() { OptionType = ItemOptionTypes.Luck }, new() { OptionType = ItemOptionTypes.GuardianOption } };
        for (var number = 1; number <= 6; number++)
        {
            options.Add(new IncreasableItemOption { OptionType = ItemOptionTypes.Excellent, Number = number });
        }

        definition.Setup(d => d.PossibleOptions).Returns(options);
        var itemDefinition = new Mock<ItemDefinition> { CallBase = true };
        itemDefinition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition> { definition.Object });
        itemDefinition.Object.Group = 8;
        itemDefinition.Object.Number = 29;
        itemDefinition.Object.Durability = 80;
        var links = new List<ItemOptionLink>();
        var item = new Mock<Item> { CallBase = true };
        item.Setup(i => i.ItemOptions).Returns(links);
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        item.Object.Definition = itemDefinition.Object;

        GkzResetSetDeliveryPlugIn.ApplyOptions(item.Object, () => new ItemOptionLink());

        Assert.That(item.Object.Level, Is.EqualTo(13));
        Assert.That(links.Single(l => l.ItemOption == normal.Object).Level, Is.EqualTo(7));
        Assert.That(links.Count(l => l.ItemOption!.OptionType == ItemOptionTypes.Excellent), Is.EqualTo(3));
        Assert.That(links.Where(l => l.ItemOption!.OptionType == ItemOptionTypes.Excellent).Select(l => l.ItemOption!.Number), Is.EquivalentTo(new[] { 3, 4, 6 }));
        Assert.That(links.Any(l => l.ItemOption!.OptionType == ItemOptionTypes.Luck), Is.True);
        Assert.That(links.Any(l => l.ItemOption!.OptionType == ItemOptionTypes.GuardianOption), Is.True);
    }

    private static Item CreateWings(DateTime? expiresAt)
    {
        var item = new Mock<Item> { CallBase = true };
        item.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        item.Object.Definition = new ItemDefinition { Group = 12, Number = 2, Durability = 200 };
        item.Object.Level = 3;
        item.Object.ExpiresAt = expiresAt;
        return item.Object;
    }

    private static void Create(GkzWelcomeKitPlugIn plugIn, Account account, Character character)
    {
        var context = new Mock<IContext>();
        context.Setup(c => c.CreateNew<Item>(It.IsAny<object?[]>())).Returns(() => new Item());
        plugIn.CharacterCreated(account, character, context.Object, CreateConfiguration(), NullLogger.Instance);
    }

    private static Character CreateCharacter(int classNumber)
    {
        var inventory = new Mock<ItemStorage> { CallBase = true };
        inventory.Setup(i => i.Items).Returns(new List<Item>());
        return new Character
        {
            CharacterClass = new CharacterClass { Number = (byte)classNumber },
            Inventory = inventory.Object,
        };
    }

    private static GameConfiguration CreateConfiguration()
    {
        var items = new List<ItemDefinition>();
        foreach (var (group, number) in new (byte Group, short Number)[] { (12, 0), (12, 1), (12, 2), (13, 30) })
        {
            items.Add(new ItemDefinition { Group = group, Number = number, Width = 3, Height = 2, Durability = 255 });
        }

        var configuration = new Mock<GameConfiguration> { CallBase = true };
        configuration.Setup(c => c.Items).Returns(items);
        return configuration.Object;
    }
}
