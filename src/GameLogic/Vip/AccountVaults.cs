// <copyright file="AccountVaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Vip;

using MUnique.OpenMU.Persistence;

/// <summary>
/// The vaults of an account. MU GKZ has up to six vaults; the first one is the original <see cref="Account.Vault"/>.
/// </summary>
public static class AccountVaults
{
    /// <summary>
    /// The most vaults an account can have.
    /// </summary>
    public const int MaximumVaults = 6;

    /// <summary>
    /// Determines whether the account is VIP now.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <returns><c>true</c>, if the VIP didn't end yet.</returns>
    public static bool IsVip(this Account account) => account.VipUntil > DateTime.UtcNow;

    /// <summary>
    /// Gets the number of vaults which the account can open now.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The number of vaults, at least 1.</returns>
    public static int GetAvailableVaults(this Account account, IGameContext gameContext)
    {
        if (VipPlugIn.GetConfiguration(gameContext) is not { } configuration)
        {
            return 1;
        }

        var count = account.IsVip() ? configuration.VipVaults : configuration.NormalVaults;
        return Math.Clamp(count, 1, MaximumVaults);
    }

    /// <summary>
    /// Gets the item storage of a vault, and creates it when the account doesn't have it yet.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <param name="number">The number of the vault, 1 to <see cref="MaximumVaults"/>.</param>
    /// <param name="context">The persistence context which creates a missing storage.</param>
    /// <returns>The item storage of the vault.</returns>
    public static ItemStorage GetOrCreateVault(this Account account, int number, IContext context)
    {
        return number switch
        {
            1 => account.Vault ??= context.CreateNew<ItemStorage>(),
            2 => account.Vault2 ??= context.CreateNew<ItemStorage>(),
            3 => account.Vault3 ??= context.CreateNew<ItemStorage>(),
            4 => account.Vault4 ??= context.CreateNew<ItemStorage>(),
            5 => account.Vault5 ??= context.CreateNew<ItemStorage>(),
            6 => account.Vault6 ??= context.CreateNew<ItemStorage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(number), number, null),
        };
    }
}
