-- MU GKZ: todos los monstruos normales de los mapas de farmeo reaparecen cada 10 segundos (antes había de 3, 8, 15, 20
-- y 50 segundos). Los jefes (150 segundos o más) y los monstruos de los eventos quedan como están.
\set ON_ERROR_STOP on
begin;
update config."MonsterDefinition" md set "RespawnDelay" = interval '10 seconds'
where md."ObjectKind" = 0 and md."RespawnDelay" <> interval '10 seconds' and md."RespawnDelay" < interval '150 seconds'
  and exists (select 1 from config."MonsterSpawnArea" sa join config."GameMapDefinition" m on m."Id" = sa."GameMapId"
              where sa."MonsterDefinitionId" = md."Id" and sa."SpawnTrigger" = 0
                and m."Number" in (0, 1, 2, 3, 4, 6, 7, 8, 10, 24, 25, 26, 27, 28, 29, 31, 33, 34, 36, 37, 38, 41, 42));
select extract(epoch from md."RespawnDelay")::int as segundos, count(distinct md."Id") as monstruos
from config."MonsterDefinition" md join config."MonsterSpawnArea" sa on sa."MonsterDefinitionId" = md."Id" and sa."SpawnTrigger" = 0
join config."GameMapDefinition" m on m."Id" = sa."GameMapId"
where md."ObjectKind" = 0 and m."Number" in (0, 1, 2, 3, 4, 6, 7, 8, 10, 24, 25, 26, 27, 28, 29, 31, 33, 34, 36, 37, 38, 41, 42)
group by 1 order by 1;
commit;
