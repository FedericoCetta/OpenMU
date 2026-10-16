-- MU GKZ: las cajas de Blood Castle caen al piso, solo para el ganador (el que entrega el arma), una vez;
-- los primeros 10 segundos las levanta él y después cualquiera (la party se las reparte).
-- Zen al doble: unos 12M por reset en lugar de 6M (el reset cuesta 5M).
begin;
update config."MiniGameReward" r set "RewardType" = 5, "RequiredSuccess" = 17
from config."MiniGameDefinition" m
where m."Id" = r."MiniGameDefinitionId" and m."Type" = 2 and r."RewardType" in (4, 5);
update config."ConstValueAttribute" c set "Value" = 0.0092
from config."AttributeDefinition" a
where a."Id" = c."DefinitionId" and a."Designation" = 'Money Drop Amount Rate' and c."GameConfigurationId" is not null;
select 'BC', m."GameLevel", r."RewardType", r."RequiredSuccess", r."RewardAmount" from config."MiniGameReward" r join config."MiniGameDefinition" m on m."Id" = r."MiniGameDefinitionId"
where m."Type" = 2 and r."ItemRewardId" is not null order by 2;
select 'zen', c."Value" from config."ConstValueAttribute" c join config."AttributeDefinition" a on a."Id" = c."DefinitionId"
where a."Designation" = 'Money Drop Amount Rate' and c."GameConfigurationId" is not null;
commit;
