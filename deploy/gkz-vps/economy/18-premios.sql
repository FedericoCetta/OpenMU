-- MU GKZ: premios de Blood Castle, Kundun (Kalima), Kanturu, Box of Kundun, monstruos dorados y drops de los mapas bajos.
begin;

-- Crea (o rehace) un grupo de drop con su lista de ítems. Devuelve el Id.
create or replace function pg_temp.grupo(p_id uuid, p_desc text, p_chance float8, p_tipo int, p_nivel int, p_items int[][]) returns uuid as $$
begin
  delete from config."DropItemGroupItemDefinition" where "DropItemGroupId" = p_id;
  delete from config."DropItemGroup" where "Id" = p_id;
  insert into config."DropItemGroup" ("Id", "GameConfigurationId", "Description", "Chance", "ItemLevel", "ItemType")
  values (p_id, '00000001-0001-0000-0000-000000000000', p_desc, p_chance, p_nivel, p_tipo);
  if p_items is not null then
    insert into config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
    select p_id, d."Id" from config."ItemDefinition" d
    where exists (select 1 from generate_subscripts(p_items, 1) k where d."Group" = p_items[k][1] and d."Number" = p_items[k][2]);
  end if;
  return p_id;
end $$ language plpgsql;

-- Une un grupo a un monstruo (por número).
create or replace function pg_temp.al_monstruo(p_monstruo int, p_grupo uuid) returns void as $$
  insert into config."MonsterDefinitionDropItemGroup" ("MonsterDefinitionId", "DropItemGroupId")
  select n."Id", p_grupo from config."MonsterDefinition" n where n."Number" = p_monstruo
  on conflict do nothing;
$$ language sql;

-- Tipos de ítem especial: 0 ninguno (de la lista), 1 ancient, 2 excellent, 6 joya.
-- La Box of Kundun +N es la Box of Luck (14/11) de nivel 7+N.

-- 1) Blood Castle: el ganador recibe cajas de Kundun en el inventario.
do $$
declare
  bc record;
  g uuid;
begin
  for bc in select * from (values (1, 2, 5), (2, 2, 5), (3, 3, 5), (4, 4, 4), (5, 4, 5), (6, 5, 4), (7, 5, 5), (8, 5, 5)) v(nivel, caja, cantidad) loop
    g := ('6c1f0e2b-0000-4a7e-8b25-b10000000' || lpad(bc.nivel::text, 3, '0'))::uuid;
    perform pg_temp.grupo(g, 'GKZ Blood Castle ' || bc.nivel || ': Box of Kundun +' || bc.caja, 1, 0, 7 + bc.caja, array[[14, 11]]);
    update config."MiniGameReward" r set "ItemRewardId" = g, "RewardAmount" = bc.cantidad, "RewardType" = 4
    from config."MiniGameDefinition" m
    where m."Id" = r."MiniGameDefinitionId" and m."Type" = 2 and m."GameLevel" = bc.nivel and r."RewardType" in (4, 5);
  end loop;
end $$;

-- Las joyas al azar de los jefes.
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b20000000001', 'GKZ jefe: joya al azar A', 1, 0, 0, array[[12, 15], [14, 13], [14, 14], [14, 16], [14, 22]]);
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b20000000002', 'GKZ jefe: joya al azar B', 1, 0, 0, array[[12, 15], [14, 13], [14, 14], [14, 16], [14, 22]]);

-- 2) Kundun (Kalima 7): 1 ancient y 2 anillos o pendientes.
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b30000000001', 'GKZ Kundun: ancient', 1, 1, 0, null);
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b30000000002', 'GKZ Kundun: anillo o pendiente A', 1, 0, 0,
  array[[13, 8], [13, 9], [13, 12], [13, 13], [13, 21], [13, 22], [13, 23], [13, 24], [13, 25], [13, 26], [13, 27], [13, 28]]);
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b30000000003', 'GKZ Kundun: anillo o pendiente B', 1, 0, 0,
  array[[13, 8], [13, 9], [13, 12], [13, 13], [13, 21], [13, 22], [13, 23], [13, 24], [13, 25], [13, 26], [13, 27], [13, 28]]);
select pg_temp.al_monstruo(275, '6c1f0e2b-0000-4a7e-8b25-b30000000001');
select pg_temp.al_monstruo(275, '6c1f0e2b-0000-4a7e-8b25-b30000000002');
select pg_temp.al_monstruo(275, '6c1f0e2b-0000-4a7e-8b25-b30000000003');
update config."MonsterDefinition" set "NumberOfMaximumItemDrops" = 3 where "Number" = 275;

-- 3) Kanturu. Nightmare: 1 ítem 380, 2 joyas al azar y 1 Jewel of Guardian. Maya: 1 excellent y 2 joyas al azar.
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b40000000001', 'GKZ Nightmare: ítem 380', 1, 0, 0,
  array[[0, 22], [0, 23], [2, 14], [4, 21], [5, 12],
        [7, 29], [7, 30], [7, 31], [7, 33], [8, 29], [8, 30], [8, 31], [8, 32], [8, 33],
        [9, 29], [9, 30], [9, 31], [9, 32], [9, 33], [10, 29], [10, 30], [10, 31], [10, 32], [10, 33],
        [11, 29], [11, 30], [11, 31], [11, 32], [11, 33]]);
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b40000000002', 'GKZ Nightmare: Jewel of Guardian', 1, 0, 0, array[[14, 31]]);
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b40000000003', 'GKZ Maya: excellent', 1, 2, 0, null);
select pg_temp.al_monstruo(361, '6c1f0e2b-0000-4a7e-8b25-b40000000001');
select pg_temp.al_monstruo(361, '6c1f0e2b-0000-4a7e-8b25-b20000000001');
select pg_temp.al_monstruo(361, '6c1f0e2b-0000-4a7e-8b25-b20000000002');
select pg_temp.al_monstruo(361, '6c1f0e2b-0000-4a7e-8b25-b40000000002');
update config."MonsterDefinition" set "NumberOfMaximumItemDrops" = 4 where "Number" = 361;
select pg_temp.al_monstruo(364, '6c1f0e2b-0000-4a7e-8b25-b40000000003');
select pg_temp.al_monstruo(364, '6c1f0e2b-0000-4a7e-8b25-b20000000001');
select pg_temp.al_monstruo(364, '6c1f0e2b-0000-4a7e-8b25-b20000000002');
update config."MonsterDefinition" set "NumberOfMaximumItemDrops" = 3 where "Number" = 364;

-- 4) Box of Kundun: nunca zen. Siempre un excellent; desde la +3, chance de ancient en su lugar.
--    Solo ítems de las clases de Season 3 (DW, DK, Elfa, MG, DL).
delete from config."ItemDropItemGroup" g using config."ItemDefinition" d
where d."Id" = g."ItemDefinitionId" and d."Group" = 14 and d."Number" = 11 and g."SourceItemLevel" between 8 and 12 and g."ItemType" = 5;
update config."ItemDropItemGroup" g set "Chance" = case g."SourceItemLevel" when 10 then 0.95 when 11 then 0.9 when 12 then 0.8 else 1 end
from config."ItemDefinition" d
where d."Id" = g."ItemDefinitionId" and d."Group" = 14 and d."Number" = 11 and g."SourceItemLevel" between 8 and 12 and g."ItemType" = 2;
delete from config."ItemDropItemGroupItemDefinition" x
using config."ItemDropItemGroup" g, config."ItemDefinition" caja
where g."Id" = x."ItemDropItemGroupId" and caja."Id" = g."ItemDefinitionId" and caja."Group" = 14 and caja."Number" = 11 and g."SourceItemLevel" between 8 and 12
  and not exists (select 1 from config."ItemDefinitionCharacterClass" c join config."CharacterClass" cc on cc."Id" = c."CharacterClassId"
                  where c."ItemDefinitionId" = x."ItemDefinitionId" and cc."Number" in (0, 4, 8, 12, 16));
insert into config."ItemDropItemGroup" ("Id", "ItemDefinitionId", "Description", "Chance", "ItemType", "SourceItemLevel", "MoneyAmount", "MinimumLevel", "MaximumLevel", "RequiredCharacterLevel", "DropEffect")
select ('6c1f0e2b-0000-4a7e-8b25-b500000000' || v.nivel)::uuid, d."Id", 'GKZ Box of Kundun +' || (v.nivel - 7) || ': ancient', v.chance, 1, v.nivel, 0, 0, 0, 0, 0
from config."ItemDefinition" d, (values (10, 0.05), (11, 0.10), (12, 0.20)) v(nivel, chance)
where d."Group" = 14 and d."Number" = 11
on conflict ("Id") do update set "Chance" = excluded."Chance";

-- 5) Monstruos dorados: 3 cajas de Kundun de su nivel cada uno.
do $$
declare
  m record;
  original uuid;
  g uuid;
  k int;
begin
  for m in select * from (values (78, 8), (53, 9), (79, 10), (80, 11), (82, 12)) v(numero, nivel) loop
    select l."DropItemGroupId" into original from config."MonsterDefinitionDropItemGroup" l join config."MonsterDefinition" n on n."Id" = l."MonsterDefinitionId"
    where n."Number" = m.numero limit 1;
    for k in 2..3 loop
      g := ('6c1f0e2b-0000-4a7e-8b25-b6000' || lpad(m.numero::text, 3, '0') || '000' || k)::uuid;
      perform pg_temp.grupo(g, 'GKZ dorado ' || m.numero || ': Box of Kundun +' || (m.nivel - 7) || ' (' || k || ')', 1, 0, m.nivel, array[[14, 11]]);
      perform pg_temp.al_monstruo(m.numero, g);
    end loop;
    update config."MonsterDefinition" set "NumberOfMaximumItemDrops" = 3 where "Number" = m.numero;
  end loop;
end $$;

-- 6) Mapas bajos (Lorencia, Dungeon, Devias, Noria, Lost Tower): piezas de sets +0, con o sin luck, nada excellent.
select pg_temp.grupo('6c1f0e2b-0000-4a7e-8b25-b70000000001', 'GKZ mapas bajos: piezas de sets +0', 0.12, 0, 0, null);
insert into config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
select '6c1f0e2b-0000-4a7e-8b25-b70000000001', d."Id" from config."ItemDefinition" d
where d."Group" between 7 and 11 and d."DropsFromMonsters"
  and exists (select 1 from config."ItemDefinitionCharacterClass" c join config."CharacterClass" cc on cc."Id" = c."CharacterClassId"
              where c."ItemDefinitionId" = d."Id" and cc."Number" in (0, 4, 8, 12, 16));
delete from config."GameMapDefinitionDropItemGroup" l
using config."GameMapDefinition" m, config."DropItemGroup" g
where m."Id" = l."GameMapDefinitionId" and g."Id" = l."DropItemGroupId" and m."Number" in (0, 1, 2, 3, 4)
  and (g."Description" like 'The common drop item group for random items%' or g."Description" like 'The common drop item group for random excellent items%');
insert into config."GameMapDefinitionDropItemGroup" ("GameMapDefinitionId", "DropItemGroupId")
select m."Id", '6c1f0e2b-0000-4a7e-8b25-b70000000001' from config."GameMapDefinition" m where m."Number" in (0, 1, 2, 3, 4)
on conflict do nothing;

-- Control.
select 'BC', m."GameLevel", r."RewardType", r."RewardAmount", g."ItemLevel" from config."MiniGameReward" r join config."MiniGameDefinition" m on m."Id" = r."MiniGameDefinitionId"
  join config."DropItemGroup" g on g."Id" = r."ItemRewardId" where m."Type" = 2 order by 2;
select 'jefe', n."Number", n."NumberOfMaximumItemDrops", string_agg(g."Description" || ' [' || (select count(*) from config."DropItemGroupItemDefinition" x where x."DropItemGroupId" = g."Id") || ']', ' | ')
from config."MonsterDefinition" n join config."MonsterDefinitionDropItemGroup" l on l."MonsterDefinitionId" = n."Id" join config."DropItemGroup" g on g."Id" = l."DropItemGroupId"
where n."Number" in (275, 361, 364, 78, 53, 79, 80, 82) group by 2, 3 order by 2;
select 'caja', g."SourceItemLevel" - 7, g."ItemType", g."Chance", (select count(*) from config."ItemDropItemGroupItemDefinition" x where x."ItemDropItemGroupId" = g."Id")
from config."ItemDropItemGroup" g join config."ItemDefinition" d on d."Id" = g."ItemDefinitionId" where d."Group" = 14 and d."Number" = 11 and g."SourceItemLevel" between 8 and 12 order by 2, 3;
select 'mapa', m."Number", string_agg(left(g."Description", 40), ' | ') from config."GameMapDefinitionDropItemGroup" l join config."GameMapDefinition" m on m."Id" = l."GameMapDefinitionId"
  join config."DropItemGroup" g on g."Id" = l."DropItemGroupId" where m."Number" in (0, 1, 2, 3, 4) and (g."Description" like 'GKZ mapas%' or g."Description" like 'The common drop item group for random%') group by 2 order by 2;
select 'piezas de sets', count(*) from config."DropItemGroupItemDefinition" where "DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-b70000000001';
commit;
