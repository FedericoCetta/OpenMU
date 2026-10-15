// <copyright file="NpcDialogBackupTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;

/// <summary>
/// Tests that the inventory backup of a crafting NPC (chaos goblin) can't be restored after its dialog
/// has been closed, which duplicated zen: talk to the goblin, close, put zen into the vault, relog.
/// </summary>
[TestFixture]
public class NpcDialogBackupTest
{
    /// <summary>
    /// Tests that closing the dialog of a crafting NPC discards the backup.
    /// </summary>
    [Test]
    public async Task ClosingTheCraftingDialogDiscardsTheBackupAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        OpenCraftingNpc(player);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.NpcDialogOpened).ConfigureAwait(false);
        player.BackupInventory = new BackupItemStorage(player.Inventory!.ItemStorage);

        await new CloseNpcDialogAction().CloseNpcDialogAsync(player).ConfigureAwait(false);

        Assert.That(player.BackupInventory, Is.Null);
    }

    /// <summary>
    /// Tests that a stale backup (no crafting dialog open) doesn't bring back the zen of the backup at the logout.
    /// </summary>
    [Test]
    public async Task StaleBackupIsNotRestoredAtLogoutAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var storage = player.Inventory!.ItemStorage;
        storage.Money = 1_000_000;
        player.BackupInventory = new BackupItemStorage(storage);

        // The zen goes elsewhere (e.g. into the vault) after the dialog is gone.
        storage.Money = 0;
        player.OpenedNpc = null;

        await player.RemoveFromGameAsync().ConfigureAwait(false);

        Assert.That(storage.Money, Is.EqualTo(0));
        Assert.That(player.BackupInventory, Is.Null);
    }

    private static void OpenCraftingNpc(Player player)
    {
        var definition = new MUnique.OpenMU.Persistence.BasicModel.MonsterDefinition { NpcWindow = NpcWindow.ChaosMachine };
        definition.ItemCraftings.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemCrafting());
        player.OpenedNpc = new NonPlayerCharacter(null!, definition, null!);
    }
}
