-- MU GKZ stage 2, standard v1: zen per reset, denser spots on the farming maps, Crywolf from level 280.
\set ON_ERROR_STOP on
begin;

-- Zen: the money of a kill is its experience; with exp x300 that's ~1300M per reset. The global rate brings it
-- to ~6M per reset for a normal player (the reset costs 5M); VIP x1.5, each zen option of the set x1.4.
update config."ConstValueAttribute" c set "Value" = 0.0046
from config."AttributeDefinition" a
where a."Id" = c."DefinitionId" and a."Designation" = 'Money Drop Amount Rate' and c."GameConfigurationId" is not null;

-- Denser spots: on the farming maps, the 3 strongest regular monsters (respawn up to 60 s) of each map get 50 % more.
with monster_levels as (
    select md."Id", ma."Value" as level
    from config."MonsterDefinition" md
    join config."MonsterAttribute" ma on ma."MonsterDefinitionId" = md."Id"
    join config."AttributeDefinition" ad on ad."Id" = ma."AttributeDefinitionId" and ad."Designation" = 'Level'
    where md."ObjectKind" = 0 and md."RespawnDelay" <= interval '60 seconds'),
ranked as (
    select sa."Id" as area_id,
           dense_rank() over (partition by sa."GameMapId" order by ml.level desc) as strength
    from config."MonsterSpawnArea" sa
    join config."GameMapDefinition" m on m."Id" = sa."GameMapId"
    join monster_levels ml on ml."Id" = sa."MonsterDefinitionId"
    where sa."SpawnTrigger" = 0
      and m."Number" in (4, 7, 8, 10, 33, 34, 37, 38))
update config."MonsterSpawnArea" sa
set "Quantity" = ceil(sa."Quantity" * 1.5)
from ranked r
where r.area_id = sa."Id" and r.strength <= 3;

-- Crywolf: the endgame spot, from level 280 by warp and through the gate of the Valley of Loren.
update config."WarpInfo" set "LevelRequirement" = 280 where "Index" = 24;
update config."EnterGate" eg set "LevelRequirement" = 280
from config."ExitGate" g, config."GameMapDefinition" m
where g."Id" = eg."TargetGateId" and m."Id" = g."MapId" and m."Number" = 34;

select 'zen rate' as what, c."Value"::text from config."ConstValueAttribute" c join config."AttributeDefinition" a on a."Id" = c."DefinitionId" where a."Designation" = 'Money Drop Amount Rate' and c."GameConfigurationId" is not null
union all select 'crywolf warp level', "LevelRequirement"::text from config."WarpInfo" where "Index" = 24
union all select 'crywolf gate level', string_agg(eg."LevelRequirement"::text, ',') from config."EnterGate" eg join config."ExitGate" g on g."Id" = eg."TargetGateId" join config."GameMapDefinition" m on m."Id" = g."MapId" where m."Number" = 34;
commit;
