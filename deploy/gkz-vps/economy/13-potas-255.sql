-- MU GKZ: las tiendas venden las pociones en pilas de 255 (manzana, curación, maná y antídoto).
-- Antes cada poción estaba dos veces (pila de 1 y de 3); queda una sola por tipo y nivel.
begin;
create temp table potas on commit drop as
select i."Id", i."ItemStorageId", i."DefinitionId", i."Level", i."ItemSlot",
       row_number() over (partition by i."ItemStorageId", i."DefinitionId", i."Level" order by i."ItemSlot") as orden
from data."Item" i join config."ItemDefinition" d on d."Id" = i."DefinitionId"
where i."ItemStorageId" in (select "MerchantStoreId" from config."MonsterDefinition" where "MerchantStoreId" is not null)
  and d."Group" = 14 and d."Number" in (0, 1, 2, 3, 4, 5, 6, 8);
delete from data."Item" where "Id" in (select "Id" from potas where orden > 1);
update data."Item" set "Durability" = 255 where "Id" in (select "Id" from potas where orden = 1);
select 'borradas', count(*) from potas where orden > 1;
select 'en 255', count(*) from potas where orden = 1;
commit;
