-- MU GKZ: el reset ya no borra las estadísticas (pedido por Federico, 2026-11-02).
-- Se mantienen las stats y los puntos sin usar, y el reset no da puntos extra: solo se ganan subiendo
-- de nivel. Con 379 niveles por reset, las clases llegan al full (32767) cerca de los 75 resets:
--   DW, DK y Elfa, 5 puntos por nivel: 70 resets (VIP, que resetea en el 360: 73).
--   MG, de 7 a 5 puntos por nivel: 70 resets (VIP: 73).
--   DL, 5 estadísticas, de 7 a 6 puntos por nivel: 72 resets (VIP: 76).
-- Reemplaza a 20-puntos-reset-150.sql. Toma efecto al reiniciar.

begin;

update config."PlugInConfiguration"
set "CustomConfiguration" = regexp_replace(regexp_replace(regexp_replace(regexp_replace("CustomConfiguration",
        '"ResetStats":\s*true', '"ResetStats": false'),
        '"PointsPerReset":\s*[0-9]+', '"PointsPerReset": 0'),
        '"MultiplyPointsByResetCount":\s*true', '"MultiplyPointsByResetCount": false'),
        '"ReplacePointsPerReset":\s*true', '"ReplacePointsPerReset": false')
where "Id" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';

-- Sin esto, el DL seguiría recibiendo sus puntos propios por reset.
update config."ConstValueAttribute" v set "Value" = 0
from config."AttributeDefinition" a, config."CharacterClass" c
where a."Id" = v."DefinitionId" and c."Id" = v."CharacterClassId"
  and split_part(a."Designation", '||', 1) ilike 'Points per Reset' and c."Number" in (16, 17);

update config."StatAttributeDefinition" s
set "BaseValue" = case when c."Number" in (12, 13) then 5 else 6 end
from config."AttributeDefinition" a, config."CharacterClass" c
where a."Id" = s."AttributeId" and c."Id" = s."CharacterClassId"
  and split_part(a."Designation", '||', 1) ilike 'Points per Level%' and c."Number" in (12, 13, 16, 17);

-- Cada personaje guarda su propia copia de este valor: también los que ya existen.
update data."StatAttribute" sa
set "Value" = case when c."Number" in (12, 13) then 5 else 6 end
from data."Character" ch, config."CharacterClass" c, config."AttributeDefinition" a
where sa."CharacterId" = ch."Id" and c."Id" = ch."CharacterClassId" and a."Id" = sa."DefinitionId"
  and split_part(a."Designation", '||', 1) ilike 'Points per Level%' and c."Number" in (12, 13, 16, 17);

select substring("CustomConfiguration" from '"ResetStats":\s*\w+') stats,
       substring("CustomConfiguration" from '"PointsPerReset":\s*\d+') puntos,
       substring("CustomConfiguration" from '"MultiplyPointsByResetCount":\s*\w+') multiplica,
       substring("CustomConfiguration" from '"ReplacePointsPerReset":\s*\w+') reemplaza
from config."PlugInConfiguration" where "Id" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';

select c."Number", s."BaseValue" as puntos_por_nivel
from config."StatAttributeDefinition" s
join config."AttributeDefinition" a on a."Id" = s."AttributeId"
join config."CharacterClass" c on c."Id" = s."CharacterClassId"
where split_part(a."Designation", '||', 1) ilike 'Points per Level%' and c."Number" in (0, 4, 8, 12, 13, 16, 17)
order by 1;

commit;
