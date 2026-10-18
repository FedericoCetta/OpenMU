// <copyright file="StartHappyHourChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// MU GKZ: a game master command which starts the Happy Hour now, without waiting for its timetable: /happyhour.
/// </summary>
[Guid("A47C3E92-1F6B-4D08-B5A3-6E9D2C8F1B74")]
[PlugIn]
[Display(Name = "MU GKZ: /happyhour", Description = "Starts the Happy Hour now.")]
[ChatCommandHelp(Command, CharacterStatus.GameMaster)]
public class StartHappyHourChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/happyhour";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var plugIn = player.GameContext.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<HappyHourPlugIn>().FirstOrDefault();
        if (plugIn is null)
        {
            await player.ShowBlueMessageAsync("La Happy Hour no está activa en el admin.").ConfigureAwait(false);
            return;
        }

        plugIn.ForceStart();
        await player.ShowBlueMessageAsync("La Happy Hour arranca en unos segundos y dura lo configurado.").ConfigureAwait(false);
    }
}
