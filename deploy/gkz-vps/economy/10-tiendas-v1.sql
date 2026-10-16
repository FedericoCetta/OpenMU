-- MU GKZ: tiendas v1.
-- 1) No se vende ninguna entrada a eventos: se sacan de todas las tiendas (todo cae de los monstruos).
-- 2) La Armor of Guardsman (Chaos Castle) no caía de ningún lado: cae como las otras entradas, 1 % desde nivel 2.
-- 3) Harold (Lorencia 183,137) pasa a ser la tienda del Dark Lord: cetros, Cross Shield y sets Light Plate y Adamantine.
begin;

-- 1) Entradas fuera de las tiendas de los NPC.
delete from data."Item" i
using config."ItemDefinition" d
where d."Id" = i."DefinitionId"
  and i."ItemStorageId" in (select "MerchantStoreId" from config."MonsterDefinition" where "MerchantStoreId" is not null)
  and ((d."Group" = 13 and d."Number" in (16, 17, 29, 49, 50)) or (d."Group" = 14 and d."Number" in (17, 18, 19)));

-- 2) Drop de la Armor of Guardsman, en los mismos mapas que el Blood Bone.
insert into config."DropItemGroup" ("Id", "GameConfigurationId", "Description", "Chance", "MinimumMonsterLevel", "MaximumMonsterLevel", "ItemLevel", "ItemType")
select '6c1f0e2b-0000-4a7e-8b25-a0d000000029', g."GameConfigurationId", 'Armor of Guardsman', 0.01, 2, null, 0, 0
from config."DropItemGroup" g
where split_part(g."Description", '||', 1) = 'Blood Bone' and g."MinimumMonsterLevel" = 2
  and not exists (select 1 from config."DropItemGroup" x where x."Id" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029');

insert into config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
select '6c1f0e2b-0000-4a7e-8b25-a0d000000029', d."Id"
from config."ItemDefinition" d
where d."Group" = 13 and d."Number" = 29
  and not exists (select 1 from config."DropItemGroupItemDefinition" x where x."DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029');

insert into config."GameMapDefinitionDropItemGroup" ("GameMapDefinitionId", "DropItemGroupId")
select l."GameMapDefinitionId", '6c1f0e2b-0000-4a7e-8b25-a0d000000029'
from config."GameMapDefinitionDropItemGroup" l
join config."DropItemGroup" g on g."Id" = l."DropItemGroupId"
where split_part(g."Description", '||', 1) = 'Blood Bone' and g."MinimumMonsterLevel" = 2
  and not exists (select 1 from config."GameMapDefinitionDropItemGroup" x where x."GameMapDefinitionId" = l."GameMapDefinitionId" and x."DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029');

-- 3) Tienda del Dark Lord en Harold. Las opciones (suerte y opción) se copian de un ítem de muestra del mismo tipo.
create temp table harold (grp int, num int, slot int, lvl int, muestra_npc int, muestra text) on commit drop;
insert into harold values
  (2, 8, 0, 3, 578, 'Battle Scepter'),
  (2, 9, 1, 3, 578, 'Master Scepter'),
  (7, 25, 2, 2, 250, 'Brass Helm'),
  (7, 26, 4, 3, 250, 'Brass Helm'),
  (6, 21, 6, 3, 251, 'Kite Shield'),
  (9, 25, 18, 2, 250, 'Brass Helm'),
  (9, 26, 20, 3, 250, 'Brass Helm'),
  (10, 25, 22, 2, 250, 'Brass Helm'),
  (8, 25, 24, 2, 250, 'Brass Helm'),
  (10, 26, 34, 3, 250, 'Brass Helm'),
  (11, 25, 36, 2, 250, 'Brass Helm'),
  (11, 26, 38, 3, 250, 'Brass Helm'),
  (8, 26, 48, 3, 250, 'Brass Helm');

-- Muestras de opciones, guardadas antes de vaciar la tienda de Harold.
create temp table muestra on commit drop as
select h.slot, i."HasSkill", o."ItemOptionId", o."Level", o."Index"
from harold h
join config."MonsterDefinition" n on n."Number" = h.muestra_npc
join data."Item" i on i."ItemStorageId" = n."MerchantStoreId"
join config."ItemDefinition" d on d."Id" = i."DefinitionId" and split_part(d."Name", '||', 1) = h.muestra
left join data."ItemOptionLink" o on o."ItemId" = i."Id"
where i."Id" = (select i2."Id" from data."Item" i2 join config."ItemDefinition" d2 on d2."Id" = i2."DefinitionId"
                where i2."ItemStorageId" = n."MerchantStoreId" and split_part(d2."Name", '||', 1) = h.muestra limit 1);

delete from data."Item" where "ItemStorageId" = (select "MerchantStoreId" from config."MonsterDefinition" where "Number" = 250);

create temp table nuevos on commit drop as
select gen_random_uuid() as id, h.slot, d."Id" as def, d."Durability" as dur, h.lvl,
       coalesce((select bool_or(m."HasSkill") from muestra m where m.slot = h.slot), false) as skill
from harold h join config."ItemDefinition" d on d."Group" = h.grp and d."Number" = h.num;

insert into data."Item" ("Id", "ItemStorageId", "DefinitionId", "ItemSlot", "Durability", "Level", "HasSkill", "SocketCount", "StorePrice", "PetExperience")
select x.id, (select "MerchantStoreId" from config."MonsterDefinition" where "Number" = 250), x.def, x.slot, x.dur, x.lvl, x.skill, 0, null, 0
from nuevos x;

insert into data."ItemOptionLink" ("Id", "ItemOptionId", "ItemId", "Level", "Index")
select gen_random_uuid(), m."ItemOptionId", x.id, m."Level", m."Index"
from nuevos x join muestra m on m.slot = x.slot
where m."ItemOptionId" is not null;

-- Control.
select split_part(n."Designation", '||', 1) as tienda, count(*) as items,
       string_agg(split_part(d."Name", '||', 1) || case when i."Level" > 0 then ' +' || i."Level" else '' end, ', ' order by i."ItemSlot") filter (where n."Number" in (250, 255, 244)) as detalle
from data."Item" i join config."MonsterDefinition" n on n."MerchantStoreId" = i."ItemStorageId"
join config."ItemDefinition" d on d."Id" = i."DefinitionId"
where exists (select 1 from config."MonsterSpawnArea" s where s."MonsterDefinitionId" = n."Id")
group by n."Number", n."Designation" order by n."Number";
select 'opciones Harold', count(*) from data."ItemOptionLink" o join nuevos x on x.id = o."ItemId";
select 'mapas Armor of Guardsman', count(*) from config."GameMapDefinitionDropItemGroup" where "DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029';
commit;
