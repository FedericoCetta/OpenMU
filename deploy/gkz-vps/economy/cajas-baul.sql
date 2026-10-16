-- MU GKZ: llena las casillas libres del baúl de las cuentas de prueba con Box of Kundun +1 a +5 (para probar el drop).
-- La cuenta tiene que estar desconectada.
begin;
create temp table cuentas on commit drop as
select a."VaultId" as baul from data."Account" a where a."LoginName" in ('fedemugm', 'juano', 'barney');
create temp table ocupadas on commit drop as
select i."ItemStorageId" as baul, i."ItemSlot" + (dx + dy * 8) as casilla
from data."Item" i join config."ItemDefinition" d on d."Id" = i."DefinitionId",
     generate_series(0, 7) dx, generate_series(0, 14) dy
where i."ItemStorageId" in (select baul from cuentas) and dx < d."Width" and dy < d."Height";
create temp table libres on commit drop as
select c.baul, s as casilla, row_number() over (partition by c.baul order by s) - 1 as n
from cuentas c, generate_series(0, 119) s
where not exists (select 1 from ocupadas o where o.baul = c.baul and o.casilla = s);
insert into data."Item" ("Id", "ItemStorageId", "DefinitionId", "ItemSlot", "Durability", "Level", "HasSkill", "SocketCount", "StorePrice", "PetExperience")
select gen_random_uuid(), l.baul, d."Id", l.casilla, 1, 8 + (l.n % 5), false, 0, null, 0
from libres l, config."ItemDefinition" d where d."Group" = 14 and d."Number" = 11;
select a."LoginName", i."Level" - 7 as caja, count(*) from data."Item" i join data."Account" a on a."VaultId" = i."ItemStorageId"
join config."ItemDefinition" d on d."Id" = i."DefinitionId" where d."Group" = 14 and d."Number" = 11 and a."LoginName" in ('fedemugm', 'juano', 'barney')
group by 1, 2 order by 1, 2;
commit;
