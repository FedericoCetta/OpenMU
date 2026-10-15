// <copyright file="GkzGuardFlags.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AntiCheat;

/// <summary>
/// What the anti-cheat of the MU GKZ client detected. The same values are in MuMain src/source/Security/GkzGuard.h.
/// </summary>
[Flags]
public enum GkzGuardFlags : uint
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>A debugger is attached to the game.</summary>
    Debugger = 1 << 0,

    /// <summary>Hardware breakpoints are set in the game (used by debuggers and cheat tools).</summary>
    HardwareBreakpoints = 1 << 1,

    /// <summary>The code of the game was changed in memory.</summary>
    CodeModified = 1 << 2,

    /// <summary>A forbidden program runs (e.g. Cheat Engine); the detail is its index in the list of the client.</summary>
    ForbiddenProgram = 1 << 3,

    /// <summary>A forbidden module is loaded into the game (e.g. the speedhack of Cheat Engine).</summary>
    ForbiddenModule = 1 << 4,
}
