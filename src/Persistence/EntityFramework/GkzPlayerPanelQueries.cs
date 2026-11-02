// <copyright file="GkzPlayerPanelQueries.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework;

using System.Threading;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The queries of the player panel of the MU GKZ website.
/// </summary>
/// <remarks>
/// The values of the visitors are always parameters (interpolated <see cref="FormattableString"/>s,
/// which EF Core sends as SQL parameters), never part of the SQL text. The changes are single
/// UPDATEs of the changed columns, so they don't touch anything else of the account.
/// The ids are the ones of <c>Stats.Level</c> and <c>Stats.Resets</c>.
/// </remarks>
public sealed class GkzPlayerPanelQueries : IGkzPlayerPanelQueries
{
    /// <inheritdoc />
    public async Task<GkzPanelAccount?> GetAccountAsync(string loginName, CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        var accounts = await context.Database.SqlQuery<GkzPanelAccount>($"""
            SELECT a."Id", a."LoginName", a."PasswordHash", a."EMail", a."State", a."RegistrationDate", a."VipUntil",
                   a."WCoinC" AS "Coins", 0 AS "PendingCoins", (a."IsBot" OR a."IsTemplate") AS "IsBotOrTemplate"
            FROM data."Account" a WHERE a."LoginName" = {loginName}
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return accounts.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<GkzPanelAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        var accounts = await context.Database.SqlQuery<GkzPanelAccount>($"""
            SELECT a."Id", a."LoginName", a."PasswordHash", a."EMail", a."State", a."RegistrationDate", a."VipUntil",
                   a."WCoinC" AS "Coins",
                   COALESCE((SELECT SUM(g."Amount") FROM data."CashShopCoinGrant" g
                             WHERE g."AccountId" = a."Id" AND g."CoinType" = 0 AND g."AppliedAt" IS NULL), 0)::int AS "PendingCoins",
                   (a."IsBot" OR a."IsTemplate") AS "IsBotOrTemplate"
            FROM data."Account" a WHERE a."Id" = {accountId}
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return accounts.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GkzPanelCharacter>> GetCharactersAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        return await context.Database.SqlQuery<GkzPanelCharacter>($"""
            SELECT c."Name", cc."Number" AS "ClassNumber",
                   COALESCE(lv."Value", 1)::int AS "Level", COALESCE(rs."Value", 0)::int AS "Resets",
                   m."Name" AS "Map", c."PlayerKillCount" AS "Kills", g."Name" AS "Guild"
            FROM data."Character" c
            JOIN config."CharacterClass" cc ON cc."Id" = c."CharacterClassId"
            LEFT JOIN config."GameMapDefinition" m ON m."Id" = c."CurrentMapId"
            LEFT JOIN data."StatAttribute" lv ON lv."CharacterId" = c."Id" AND lv."DefinitionId" = '560931ad-0901-4342-b7f4-fd2e2fcc0563'
            LEFT JOIN data."StatAttribute" rs ON rs."CharacterId" = c."Id" AND rs."DefinitionId" = '89a891a7-f9f9-4ab5-af36-12056e53a5f7'
            LEFT JOIN guild."GuildMember" gm ON gm."Id" = c."Id"
            LEFT JOIN guild."Guild" g ON g."Id" = gm."GuildId"
            WHERE c."AccountId" = {accountId}
            ORDER BY c."CharacterSlot"
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> SetPasswordHashAsync(Guid accountId, string passwordHash, CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        var rows = await context.Database.ExecuteSqlAsync(
            $"""UPDATE data."Account" SET "PasswordHash" = {passwordHash} WHERE "Id" = {accountId}""",
            cancellationToken).ConfigureAwait(false);
        return rows == 1;
    }

    /// <inheritdoc />
    public async Task<bool> SetEMailAsync(Guid accountId, string email, CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        var rows = await context.Database.ExecuteSqlAsync(
            $"""UPDATE data."Account" SET "EMail" = {email} WHERE "Id" = {accountId}""",
            cancellationToken).ConfigureAwait(false);
        return rows == 1;
    }

    /// <inheritdoc />
    public async Task<bool> MoveToLorenciaAsync(Guid accountId, string characterName, CancellationToken cancellationToken)
    {
        // To the middle of the spawn gate of Lorencia (map 0), where a character appears after dying there.
        await using var context = new EntityDataContext();
        var rows = await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE data."Character" c
            SET "CurrentMapId" = gate."MapId", "PositionX" = gate."X", "PositionY" = gate."Y"
            FROM (SELECT eg."MapId", ((eg."X1" + eg."X2") / 2)::smallint AS "X", ((eg."Y1" + eg."Y2") / 2)::smallint AS "Y"
                  FROM config."ExitGate" eg JOIN config."GameMapDefinition" m ON m."Id" = eg."MapId"
                  WHERE m."Number" = 0 AND eg."IsSpawnGate" LIMIT 1) gate
            WHERE c."AccountId" = {accountId} AND c."Name" = {characterName}
            """,
            cancellationToken).ConfigureAwait(false);
        return rows == 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GkzPanelPurchase>> GetPurchasesAsync(Guid accountId, CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        return await context.Database.SqlQuery<GkzPanelPurchase>($"""
            SELECT g."Reason", g."Amount", g."CreatedAt", g."AppliedAt" IS NOT NULL AS "IsApplied"
            FROM data."CashShopCoinGrant" g
            WHERE g."AccountId" = {accountId} AND (g."Reference" LIKE 'mp:%' OR g."Reference" LIKE 'pp:%')
            ORDER BY g."CreatedAt" DESC
            LIMIT 20
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
