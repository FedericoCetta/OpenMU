-- MU GKZ: las cajas (Box of Luck y Box of Kundun +1 a +5) se apilan hasta 64 (las joyas hasta 50); solo con el mismo nivel.
-- Abrir una caja de una pila descuenta una (DropItemAction.cs).
begin;
update config."ItemDefinition" set "Durability" = 64 where "Group" = 14 and "Number" = 11;
select split_part("Name", '||', 1), "Durability" from config."ItemDefinition" where "Group" = 14 and "Number" = 11;
-- Junta las cajas sueltas de los baúles de prueba en pilas de 64 (con el server frenado).
create temp table cajas on commit drop as
select i."Id", i."ItemStorageId", i."Level", i."ItemSlot",
       row_number() over (partition by i."ItemStorageId", i."Level" order by i."ItemSlot") - 1 as n,
       count(*) over (partition by i."ItemStorageId", i."Level") as total
from data."Item" i join config."ItemDefinition" d on d."Id" = i."DefinitionId"
where d."Group" = 14 and d."Number" = 11 and i."Durability" = 1
  and i."ItemStorageId" in (select "VaultId" from data."Account" where "LoginName" in ('fedemugm', 'juano', 'barney'));
update data."Item" i set "Durability" = least(64, c.total - c.n) from cajas c where c."Id" = i."Id" and c.n % 64 = 0;
delete from data."Item" where "Id" in (select "Id" from cajas where n % 64 <> 0);
select a."LoginName", i."Level" - 7 as caja, count(*) as pilas, sum(i."Durability") as cajas from data."Item" i join data."Account" a on a."VaultId" = i."ItemStorageId"
join config."ItemDefinition" d on d."Id" = i."DefinitionId" where d."Group" = 14 and d."Number" = 11 and a."LoginName" in ('fedemugm', 'juano', 'barney') group by 1, 2 order by 1, 2;
commit;
