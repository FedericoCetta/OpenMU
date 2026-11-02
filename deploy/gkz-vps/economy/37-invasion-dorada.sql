-- MU GKZ: la invasión dorada según el listado de Federico (2026-10-29).
-- Menos monstruos (con la vida x20 de 36-vida-invasiones.sql) y todos tiran cajas de Kundun:
--
--   Monstruo              Cantidad  Mapa                      Tira
--   Golden Budge Dragon      4      Lorencia                  2 Box of Kundun +1
--   Golden Goblin            4      Noria                     2 Box of Kundun +1
--   Golden Titan             5      Devias                    3 Box of Kundun +2
--   Golden Vepar             6      Atlans                    3 Box of Kundun +2
--   Golden Soldier           8      Devias                    3 Box of Kundun +3
--   Golden Dragon            4      Lorencia, Noria, Devias   3 Box of Kundun +3
--   Golden Wheel             6      Tarkan                    1 Box of Kundun +3
--   Golden Lizard King       3      Atlans                    3 Box of Kundun +4
--   Golden Tantallos         3      Tarkan                    3 Box of Kundun +5
--
-- Reemplaza las cajas de los dorados de 18-premios.sql (que daba 3 cajas a 78, 53, 79, 80 y 82):
-- no volver a correr esa parte de 18 después de este. Se puede correr de nuevo sin duplicar nada.
-- La Box of Kundun +N es la Box of Luck (14/11) de nivel 7+N. Toma efecto al reiniciar.

begin;

create temporary table dorados (numero int, cantidad int, cajas int, kundun int) on commit drop;
insert into dorados values
  (43, 4, 2, 1),  -- Golden Budge Dragon
  (78, 4, 2, 1),  -- Golden Goblin
  (53, 5, 3, 2),  -- Golden Titan
  (81, 6, 3, 2),  -- Golden Vepar
  (54, 8, 3, 3),  -- Golden Soldier
  (79, 4, 3, 3),  -- Golden Dragon
  (83, 6, 1, 3),  -- Golden Wheel
  (80, 3, 3, 4),  -- Golden Lizard King
  (82, 3, 3, 5);  -- Golden Tantallos

-- 1) Las cajas: se sacan todos los grupos de drop de estos monstruos y se ponen tantos grupos
--    seguros (chance 1) de una caja como cajas tira cada uno; el máximo de drops es esa cantidad.
delete from config."MonsterDefinitionDropItemGroup" l
using config."MonsterDefinition" m, dorados d
where m."Id" = l."MonsterDefinitionId" and m."Number" = d.numero;

-- Los grupos "GKZ dorado" de 18-premios.sql quedan sin uso.
delete from config."DropItemGroup" where "Description" like 'GKZ dorado %';

insert into config."DropItemGroup" ("Id", "GameConfigurationId", "Description", "Chance", "ItemLevel", "ItemType")
select ('6c1f0e2b-0000-4a7e-8b25-b6100' || lpad(d.numero::text, 3, '0') || '000' || k)::uuid,
       '00000001-0001-0000-0000-000000000000',
       'GKZ invasión dorada ' || d.numero || ': Box of Kundun +' || d.kundun || ' (' || k || ')',
       1, 7 + d.kundun, 0
from dorados d, generate_series(1, d.cajas) k;

insert into config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
select ('6c1f0e2b-0000-4a7e-8b25-b6100' || lpad(d.numero::text, 3, '0') || '000' || k)::uuid, i."Id"
from dorados d, generate_series(1, d.cajas) k, config."ItemDefinition" i
where i."Group" = 14 and i."Number" = 11;

insert into config."MonsterDefinitionDropItemGroup" ("MonsterDefinitionId", "DropItemGroupId")
select m."Id", ('6c1f0e2b-0000-4a7e-8b25-b6100' || lpad(d.numero::text, 3, '0') || '000' || k)::uuid
from dorados d join config."MonsterDefinition" m on m."Number" = d.numero, generate_series(1, d.cajas) k;

update config."MonsterDefinition" m set "NumberOfMaximumItemDrops" = d.cajas
from dorados d where m."Number" = d.numero;

-- 2) Las cantidades de la invasión dorada (GoldenInvasionPlugIn). Se cambia el texto y no el jsonb,
--    porque el jsonb reordena las claves y el "$id" tiene que quedar primero.
do $$
declare
  d record;
begin
  for d in select * from dorados loop
    update config."PlugInConfiguration"
    set "CustomConfiguration" = regexp_replace("CustomConfiguration",
          '("MonsterId": ' || d.numero || ',\s*"Count": )\d+', '\1' || d.cantidad)
    where "TypeId" = '06d18a9e-2919-4c17-9dbc-6e4f7756495c';
  end loop;
end $$;

-- Control.
select m."Number", split_part(m."Designation", '||', 1) as monstruo, m."NumberOfMaximumItemDrops" as drops,
       string_agg(split_part(g."Description", '||', 1), ' | ' order by g."Description") as cajas
from config."MonsterDefinition" m
join config."MonsterDefinitionDropItemGroup" l on l."MonsterDefinitionId" = m."Id"
join config."DropItemGroup" g on g."Id" = l."DropItemGroupId"
where m."Number" in (select numero from dorados)
group by m."Number", m."Designation", m."NumberOfMaximumItemDrops" order by m."Number";

select (x->>'MonsterId')::int as monstruo, (x->>'Count')::int as cantidad
from config."PlugInConfiguration" p, jsonb_array_elements((p."CustomConfiguration"::jsonb)->'Mobs'->'$values') x
where p."TypeId" = '06d18a9e-2919-4c17-9dbc-6e4f7756495c';

commit;
