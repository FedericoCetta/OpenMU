-- MU GKZ: tienda v6, igual que GkzCashShop.json (scriptYearId 6). Suma Jewel of Life y de Chaos, deja solo el
-- Moonstone Pendant en Anillos, saca la Armor of Guardsman y agrega los packs de Box of Kundun de la pestaña Extras.
-- Necesita el cliente con el script 512.2026.006 y reiniciar el server.
begin;

update config."CashShopConfiguration" set "ScriptYearId" = 6;

-- Lo que sale del catálogo deja de venderse; los registros quedan por si alguien lo tiene guardado.
update config."CashShopPackage" set "IsForSale" = false where "PackageSequence" in (31, 41, 42, 43, 44, 45, 46);

create temp table tienda_nueva (seq int, nombre text, precio int, grupo int, numero int, nivel int, cantidad int) on commit drop;
insert into tienda_nueva values
  (47, 'Moonstone Pendant', 100, 13, 38, 0, 1),
  (81, '10 Jewel of Life', 600, 14, 16, 0, 10),
  (82, '10 Jewel of Chaos', 300, 12, 15, 0, 10),
  (91, '30 Box of Kundun +1', 300, 14, 11, 8, 30),
  (92, '64 Box of Kundun +1', 600, 14, 11, 8, 64),
  (93, '30 Box of Kundun +2', 450, 14, 11, 9, 30),
  (94, '64 Box of Kundun +2', 900, 14, 11, 9, 64);

-- CoinType 0 es WCoinC, los GKZ Coins: el mismo de los paquetes que ya se venden.
insert into config."CashShopPackage" ("Id", "CashShopConfigurationId", "PackageSequence", "Name", "Price", "CoinType", "IsForSale", "IsGiftable", "IsBundle")
select gen_random_uuid(), c."Id", n.seq, n.nombre, n.precio, ref."CoinType", true, true, false
from tienda_nueva n
cross join config."CashShopConfiguration" c
cross join (select "CoinType" from config."CashShopPackage" where "PackageSequence" = 51) ref;

insert into config."CashShopProduct" ("Id", "ItemDefinitionId", "CashShopPackageId", "ProductSequence", "PriceSequence", "Price", "ItemLevel", "Quantity", "Duration")
select gen_random_uuid(), d."Id", p."Id", n.seq, n.seq, n.precio, n.nivel, n.cantidad, interval '0'
from tienda_nueva n
join config."CashShopPackage" p on p."PackageSequence" = n.seq
join config."ItemDefinition" d on d."Group" = n.grupo and d."Number" = n.numero;

-- Si falta un ítem o un producto, se cancela todo.
do $$
begin
  if (select count(*) from config."CashShopProduct" where "ProductSequence" in (47, 81, 82, 91, 92, 93, 94)) <> 7 then
    raise exception 'Faltan productos de la tienda nueva';
  end if;
end $$;

-- Queda registrado como la versión 6 de la actualización de la tienda.
update config."ConfigurationUpdate" set "Version" = 6, "UpdatedAt" = '2026-11-04 14:00:00+00', "InstalledAt" = now() where "Name" = 'MU GKZ: item shop';

commit;
