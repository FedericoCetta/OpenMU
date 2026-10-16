-- MU GKZ: las estadísticas llegan al tope (32767) a los 150 resets.
-- DW, DK, Elfa y MG: 874 puntos por reset (4 × 32767 menos las estadísticas iniciales, entre 150).
-- DL y Lord Emperor: 1092 puntos por reset (5 estadísticas, con comando).
begin;
update config."PlugInConfiguration"
set "CustomConfiguration" = regexp_replace("CustomConfiguration", '"PointsPerReset": [0-9]+', '"PointsPerReset": 874')
where "TypeId" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';
update config."ConstValueAttribute" v set "Value" = 1092
from config."AttributeDefinition" a, config."CharacterClass" c
where a."Id" = v."DefinitionId" and c."Id" = v."CharacterClassId" and split_part(a."Designation", '||', 1) ilike 'Points per Reset' and c."Number" in (16, 17);
select substring("CustomConfiguration" from '"PointsPerReset": [0-9]+') from config."PlugInConfiguration" where "TypeId" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';
select c."Number", v."Value" from config."ConstValueAttribute" v join config."AttributeDefinition" a on a."Id" = v."DefinitionId" join config."CharacterClass" c on c."Id" = v."CharacterClassId"
where split_part(a."Designation", '||', 1) ilike 'Points per Reset';
commit;
