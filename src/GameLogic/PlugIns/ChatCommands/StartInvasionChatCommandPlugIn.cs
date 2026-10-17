// <copyright file="StartInvasionChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// MU GKZ: a game master command which starts an invasion now, without waiting for its timetable:
/// /startinvasion white, /startinvasion golden or /startinvasion red.
/// </summary>
[Guid("8F3C6A2E-4D17-4B90-A5E1-7C2B9D0F4E63")]
[PlugIn]
[Display(Name = "MU GKZ: /startinvasion", Description = "Starts an invasion now: /startinvasion white, golden or red.")]
[ChatCommandHelp(Command, CharacterStatus.GameMaster)]
public class StartInvasionChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/startinvasion";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var argument = command.Length > Command.Length ? command[Command.Length..].Trim().ToLowerInvariant() : string.Empty;
        var invasions = player.GameContext.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>();
        IPeriodicTaskPlugIn? invasion = argument switch
        {
            "white" or "wizard" => invasions.OfType<WhiteWizardInvasionPlugIn>().FirstOrDefault(),
            "golden" or "gold" => invasions.OfType<GoldenInvasionPlugIn>().FirstOrDefault(),
            "red" or "dragon" => invasions.OfType<RedDragonInvasionPlugIn>().FirstOrDefault(),
            _ => null,
        };

        if (invasion is null)
        {
            await player.ShowBlueMessageAsync("Uso: /startinvasion white, golden o red (la invasión tiene que estar activa en el admin).").ConfigureAwait(false);
            return;
        }

        invasion.ForceStart();
        await player.ShowBlueMessageAsync($"La invasión arranca en unos segundos: {argument}.").ConfigureAwait(false);
    }
}
