-- MU GKZ: drops de ítems (decidido con Federico el 2026-10-21).
-- - Luck: 40 % -> 80 %.
-- - Excellent: la 1ª opción sigue segura; cada opción extra 10 % -> 15 % (máximo 3).
--   Queda ~28 % de los excellent con 2 opciones o más y ~2 % con 3.
-- - Entre las opciones de defensa, el reflect y la vida salen el doble de seguido (peso 2; el resto 1).
--   Requiere el server con el sorteo por peso (DefaultDropGenerator, 2026-10-21).
-- - Manos de Maya (Kanturu): tiraban 1 ítem 380 excellent y el resto salía de los drops comunes del
--   mapa (ítems sin excellent). Ahora cada tirada es un ítem 380 excellent: la mano izquierda tira 3
--   y la derecha 2, con copias del mismo grupo.
-- Las cajas (Box of Kundun) usan las mismas chances que el drop común.
-- La configuración queda en caché en el server: toma efecto al reiniciar.

begin;

update config."ItemOptionDefinition"
set "AddChance" = 0.8
where split_part("Name", '||', 1) = 'Luck';

update config."ItemOptionDefinition"
set "AddChance" = 0.15
where split_part("Name", '||', 1) in (
    'Excellent Defense Options',
    'Excellent Physical Attack Options',
    'Excellent Wizardry Attack Options');

-- Reflect (3) y vida (6) con peso 2 entre las opciones excellent de defensa.
update config."IncreasableItemOption" o
set "Weight" = case when o."Number" in (3, 6) then 2 else 0 end
from config."ItemOptionDefinition" d
where d."Id" = o."ItemOptionDefinitionId" and split_part(d."Name", '||', 1) = 'Excellent Defense Options';

-- Manos de Maya: copias 2 y 3 del grupo "ítem 380" (mismos ítems), para que cada tirada sea excellent.
create temporary table gkz_copias (id uuid, numero int) on commit drop;
insert into gkz_copias values
    ('6c1f0e2b-0000-4a7e-8b25-b40000000012', 2),
    ('6c1f0e2b-0000-4a7e-8b25-b40000000013', 3);

delete from config."MonsterDefinitionDropItemGroup" where "DropItemGroupId" in (select id from gkz_copias);
delete from config."DropItemGroupItemDefinition" where "DropItemGroupId" in (select id from gkz_copias);
delete from config."DropItemGroup" where "Id" in (select id from gkz_copias);

insert into config."DropItemGroup" ("Id", "MonsterId", "GameConfigurationId", "Description", "Chance", "MinimumMonsterLevel", "MaximumMonsterLevel", "ItemLevel", "ItemType")
select c.id, g."MonsterId", g."GameConfigurationId", g."Description" || ' (' || c.numero || ')', g."Chance", g."MinimumMonsterLevel", g."MaximumMonsterLevel", g."ItemLevel", g."ItemType"
from config."DropItemGroup" g, gkz_copias c
where g."Id" = '6c1f0e2b-0000-4a7e-8b25-b40000000001';

insert into config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
select c.id, x."ItemDefinitionId"
from config."DropItemGroupItemDefinition" x, gkz_copias c
where x."DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-b40000000001';

-- Mano izquierda (362): 3 tiradas -> grupos 1, 2 y 3. Mano derecha (363): 2 tiradas -> grupos 1 y 2.
insert into config."MonsterDefinitionDropItemGroup" ("MonsterDefinitionId", "DropItemGroupId")
select m."Id", c.id
from config."MonsterDefinition" m, gkz_copias c
where (m."Number" = 362) or (m."Number" = 363 and c.numero = 2);

select split_part("Name", '||', 1) as opcion, "AddChance", "MaximumOptionsPerItem"
from config."ItemOptionDefinition"
where split_part("Name", '||', 1) in ('Luck', 'Excellent Defense Options', 'Excellent Physical Attack Options', 'Excellent Wizardry Attack Options')
order by 1;

select m."Number", split_part(m."Designation", '||', 1) as monstruo, m."NumberOfMaximumItemDrops" as tiradas, count(*) as grupos_380
from config."MonsterDefinition" m
join config."MonsterDefinitionDropItemGroup" l on l."MonsterDefinitionId" = m."Id"
join config."DropItemGroup" g on g."Id" = l."DropItemGroupId"
where m."Number" in (362, 363) and g."Description" like 'GKZ Nightmare: ítem 380%'
group by 1, 2, 3 order by 1;

commit;
