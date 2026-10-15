-- MU GKZ stage 3, drops standard v1: jewels as currency (the same on all maps, Guardian only in Aida, Kanturu and
-- Crywolf), excellent 0.03 %, fewer random items, Fenrir materials in Crywolf and Kanturu, softer Jewel of Soul.
\set ON_ERROR_STOP on
begin;

-- Fewer random items: less junk and less zen from selling it, so the zen set keeps its value.
update config."DropItemGroup" set "Chance" = 0.12 where "Description" like 'The common drop item group for random items%';

-- Excellent items: about one every two hours of play.
update config."DropItemGroup" set "Chance" = 0.0003 where "Description" like 'The common drop item group for random excellent items%';

-- One drop group per jewel. Re-running the script replaces them.
delete from config."GameMapDefinitionDropItemGroup" where "DropItemGroupId" in (select "Id" from config."DropItemGroup" where "Description" like 'GKZ jewel: %');
delete from config."DropItemGroupItemDefinition" where "DropItemGroupId" in (select "Id" from config."DropItemGroup" where "Description" like 'GKZ jewel: %');
delete from config."DropItemGroup" where "Description" like 'GKZ jewel: %';

create temporary table gkz_jewels (name text, item_group int, item_number int, chance double precision, maps int[]) on commit drop;
insert into gkz_jewels values
    ('Jewel of Bless', 14, 13, 0.0022, null),
    ('Jewel of Soul', 14, 14, 0.0018, null),
    ('Jewel of Chaos', 12, 15, 0.0018, null),
    ('Jewel of Life', 14, 16, 0.0006, null),
    ('Jewel of Creation', 14, 22, 0.0003, null),
    ('Jewel of Guardian', 14, 31, 0.0004, array[33, 34, 37, 38]);

create temporary table gkz_groups on commit drop as
select gen_random_uuid() as id, j.* from gkz_jewels j;

insert into config."DropItemGroup" ("Id", "GameConfigurationId", "Chance", "MinimumMonsterLevel", "MaximumMonsterLevel", "ItemLevel", "ItemType", "MonsterId", "Description")
select g.id, '00000001-0001-0000-0000-000000000000', g.chance, null, null, 0, 0, null, 'GKZ jewel: ' || g.name from gkz_groups g;

insert into config."DropItemGroupItemDefinition" ("DropItemGroupId", "ItemDefinitionId")
select g.id, d."Id" from gkz_groups g join config."ItemDefinition" d on d."Group" = g.item_group and d."Number" = g.item_number;

-- On every map with regular monsters (the ones which have the common money group), or only on the listed ones.
insert into config."GameMapDefinitionDropItemGroup" ("GameMapDefinitionId", "DropItemGroupId")
select l."GameMapDefinitionId", g.id
from gkz_groups g
join config."GameMapDefinitionDropItemGroup" l on l."DropItemGroupId" = (select "Id" from config."DropItemGroup" where "Description" like 'The common money drop item group%')
join config."GameMapDefinition" m on m."Id" = l."GameMapDefinitionId"
where g.maps is null or m."Number" = any (g.maps);

-- Fenrir materials: Crywolf and Kanturu (1 and 3).
insert into config."GameMapDefinitionDropItemGroup" ("GameMapDefinitionId", "DropItemGroupId")
select m."Id", dg."Id"
from config."DropItemGroup" dg, config."GameMapDefinition" m
where split_part(dg."Description", '||', 1) in ('Splinter of Armor', 'Bless of Guardian', 'Claw of Beast') and m."Number" in (37, 38)
  and not exists (select 1 from config."GameMapDefinitionDropItemGroup" x where x."GameMapDefinitionId" = m."Id" and x."DropItemGroupId" = dg."Id");

-- Jewel of Soul: 65 % (90 % with luck); a failure takes away one level, never back to +0.
update config."PlugInConfiguration"
set "CustomConfiguration" = regexp_replace(regexp_replace(regexp_replace("CustomConfiguration",
        '"SuccessRatePercentage": [0-9]+', '"SuccessRatePercentage": 65'),
        '"SuccessRateBonusWithLuckPercentage": [0-9]+', '"SuccessRateBonusWithLuckPercentage": 25'),
        '"ResetToLevel0WhenFailMinLevel": [0-9]+', '"ResetToLevel0WhenFailMinLevel": 255')
where "TypeId" = 'a76cda49-1c56-401a-96d1-294d9a68a7b9';

select split_part(g."Description", '||', 1) as grp, g."Chance", count(l.*) as maps
from config."DropItemGroup" g left join config."GameMapDefinitionDropItemGroup" l on l."DropItemGroupId" = g."Id"
where g."Description" like 'GKZ jewel: %' or g."Description" like 'The common drop item group%' or split_part(g."Description", '||', 1) in ('Splinter of Armor', 'Bless of Guardian', 'Claw of Beast')
group by 1, 2 order by 2 desc;
select substring("CustomConfiguration" from '"SuccessRatePercentage": [0-9]+') || ', ' || substring("CustomConfiguration" from '"ResetToLevel0WhenFailMinLevel": [0-9]+')
from config."PlugInConfiguration" where "TypeId" = 'a76cda49-1c56-401a-96d1-294d9a68a7b9';
commit;
