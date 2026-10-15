-- MU GKZ stage 1 of the economy: exp x300 (VIP x500), resets at 380 (VIP 360), 350 points per reset (DL 400), 5M zen.
\set ON_ERROR_STOP on
begin;

update config."GameConfiguration" set "ExperienceRate" = 300;

-- VIP: +66.67 % experience, so x300 becomes x500.
update config."PlugInConfiguration"
set "CustomConfiguration" = regexp_replace("CustomConfiguration", '"ExperienceBonus": [0-9.]+', '"ExperienceBonus": 0.6667')
where "TypeId" = 'c3b5e2a1-7d4f-4a86-9e21-5b0c8d7f4a93';

-- Resets.
update config."PlugInConfiguration"
set "IsActive" = true,
    "CustomConfiguration" = '{
  "$id": "",
  "ResetLimit": null,
  "RequiredLevel": 380,
  "RequiredLevelVip": 360,
  "LevelAfterReset": 1,
  "RequiredMoney": 5000000,
  "MultiplyRequiredMoneyByResetCount": false,
  "RequiredResetItem": null,
  "ItemCostTiers": { "$id": "", "$values": [] },
  "ResetStats": true,
  "PointsPerReset": 350,
  "MultiplyPointsByResetCount": true,
  "ReplacePointsPerReset": true,
  "PointsTiers": { "$id": "", "$values": [] },
  "MoveHome": true,
  "LogOut": true
}'
where "TypeId" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';

-- Dark Lord and Lord Emperor: 400 points per reset instead of 350.
delete from config."ConstValueAttribute"
where "DefinitionId" = 'a34f4f57-b364-4cdb-9989-64cedd2cd831'
  and "CharacterClassId" in ('00000040-0010-0000-0000-000000000000', '00000040-0011-0000-0000-000000000000');
insert into config."ConstValueAttribute" ("Id", "DefinitionId", "CharacterClassId", "Value", "GameConfigurationId", "AggregateType")
values (gen_random_uuid(), 'a34f4f57-b364-4cdb-9989-64cedd2cd831', '00000040-0010-0000-0000-000000000000', 400, null, 0),
       (gen_random_uuid(), 'a34f4f57-b364-4cdb-9989-64cedd2cd831', '00000040-0011-0000-0000-000000000000', 400, null, 0);

select 'exp' as what, "ExperienceRate"::text from config."GameConfiguration"
union all select 'vip exp', substring("CustomConfiguration" from '"ExperienceBonus": [0-9.]+') from config."PlugInConfiguration" where "TypeId" = 'c3b5e2a1-7d4f-4a86-9e21-5b0c8d7f4a93'
union all select 'reset active', "IsActive"::text from config."PlugInConfiguration" where "TypeId" = '6a9d585d-79d7-4674-b6ea-7e87392fa501'
union all select 'dl points', count(*)::text from config."ConstValueAttribute" where "DefinitionId" = 'a34f4f57-b364-4cdb-9989-64cedd2cd831';
commit;
