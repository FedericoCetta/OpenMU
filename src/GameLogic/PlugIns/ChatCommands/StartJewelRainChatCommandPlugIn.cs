// <copyright file="StartJewelRainChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// MU GKZ: a game master command which starts the jewel rain now, without waiting for its timetable: /lluvia.
/// </summary>
[Guid("5D2A8F14-9B3E-4C71-A6D0-E83F1B7C2A95")]
[PlugIn]
[Display(Name = "MU GKZ: /lluvia", Description = "Starts the jewel rain now.")]
[ChatCommandHelp(Command, CharacterStatus.GameMaster)]
public class StartJewelRainChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/lluvia";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var plugIn = player.GameContext.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<GkzJewelRainPlugIn>().FirstOrDefault();
        if (plugIn is null)
        {
            await player.ShowBlueMessageAsync("La lluvia de joyas no está activa en el admin.").ConfigureAwait(false);
            return;
        }

        plugIn.ForceStart();
        await player.ShowBlueMessageAsync("Lluvia de joyas: el aviso sale en unos segundos y las joyas caen un minuto después.").ConfigureAwait(false);
    }
}
