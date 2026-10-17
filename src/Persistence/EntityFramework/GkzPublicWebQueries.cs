// <copyright file="GkzPublicWebQueries.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework;

using System.Threading;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The read-only queries of the public website of MU GKZ.
/// </summary>
/// <remarks>
/// Every query is a constant: no text of the visitors gets into the SQL, so nothing can be injected.
/// Only real players are ranked: no bots, templates, game masters or banned accounts and characters.
/// The ids are the ones of <c>Stats.Level</c> and <c>Stats.Resets</c>.
/// </remarks>
public sealed class GkzPublicWebQueries : IGkzPublicWebQueries
{
    private const string CharacterSelect = """
        SELECT c."Name" AS "Name", cc."Number" AS "ClassNumber",
               COALESCE(lv."Value", 1)::int AS "Level", COALESCE(rs."Value", 0)::int AS "Resets",
               c."PlayerKillCount" AS "Kills", g."Name" AS "Guild"
        FROM data."Character" c
        JOIN data."Account" a ON a."Id" = c."AccountId"
        JOIN config."CharacterClass" cc ON cc."Id" = c."CharacterClassId"
        LEFT JOIN data."StatAttribute" lv ON lv."CharacterId" = c."Id" AND lv."DefinitionId" = '560931ad-0901-4342-b7f4-fd2e2fcc0563'
        LEFT JOIN data."StatAttribute" rs ON rs."CharacterId" = c."Id" AND rs."DefinitionId" = '89a891a7-f9f9-4ab5-af36-12056e53a5f7'
        LEFT JOIN guild."GuildMember" gm ON gm."Id" = c."Id"
        LEFT JOIN guild."Guild" g ON g."Id" = gm."GuildId"
        WHERE NOT a."IsBot" AND NOT a."IsTemplate" AND a."State" = 0 AND c."CharacterStatus" = 0
        """;

    private const string ByResets = CharacterSelect + """

        ORDER BY "Resets" DESC, "Level" DESC, c."Experience" DESC, c."Name" LIMIT 50
        """;

    private const string ByLevel = CharacterSelect + """

        ORDER BY "Level" DESC, "Resets" DESC, c."Experience" DESC, c."Name" LIMIT 50
        """;

    private const string ByKills = CharacterSelect + """

          AND c."PlayerKillCount" > 0
        ORDER BY "Kills" DESC, "Resets" DESC, c."Name" LIMIT 50
        """;

    private const string GuildSelect = """
        SELECT g."Name" AS "Name", g."Score" AS "Score", g."Logo" AS "Logo",
               (SELECT COUNT(*)::int FROM guild."GuildMember" m WHERE m."GuildId" = g."Id") AS "Members",
               (SELECT c."Name" FROM guild."GuildMember" m JOIN data."Character" c ON c."Id" = m."Id"
                 WHERE m."GuildId" = g."Id" AND m."Status" = 2 LIMIT 1) AS "Master"
        FROM guild."Guild" g
        """;

    private const string GuildRanking = GuildSelect + """

        ORDER BY g."Score" DESC, "Members" DESC, g."Name" LIMIT 50
        """;

    private const string CastleOwner = GuildSelect + """

        WHERE g."Id" = (SELECT d."OwnerGuildId" FROM data."CastleSiegeData" d WHERE d."IsOccupied" LIMIT 1)
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GkzRankedCharacter>> GetCharacterRankingAsync(GkzCharacterRankingOrder order, CancellationToken cancellationToken)
    {
        var sql = order switch
        {
            GkzCharacterRankingOrder.Level => ByLevel,
            GkzCharacterRankingOrder.Kills => ByKills,
            _ => ByResets,
        };

        await using var context = new EntityDataContext();
        return await context.Database.SqlQueryRaw<GkzRankedCharacter>(sql).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GkzRankedGuild>> GetGuildRankingAsync(CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        return await context.Database.SqlQueryRaw<GkzRankedGuild>(GuildRanking).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GkzRankedGuild?> GetCastleOwnerAsync(CancellationToken cancellationToken)
    {
        await using var context = new EntityDataContext();
        var owners = await context.Database.SqlQueryRaw<GkzRankedGuild>(CastleOwner).ToListAsync(cancellationToken).ConfigureAwait(false);
        return owners.FirstOrDefault();
    }
}
