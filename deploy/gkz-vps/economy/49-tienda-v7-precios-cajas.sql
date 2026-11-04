-- MU GKZ: tienda v7, igual que GkzCashShop.json (scriptYearId 7).
-- Packs de Box of Kundun +1 y +2: 500 coins las 30 y 1000 las 64. Premio de 501 resets: un set de la clase, a elegir.
-- Necesita el cliente con el script 512.2026.007 y reiniciar el server.
begin;

update config."CashShopConfiguration" set "ScriptYearId" = 7;

update config."CashShopPackage" set "Price" = case when "PackageSequence" in (91, 93) then 500 else 1000 end
where "PackageSequence" in (91, 92, 93, 94);

update config."CashShopProduct" set "Price" = case when "ProductSequence" in (91, 93) then 500 else 1000 end
where "ProductSequence" in (91, 92, 93, 94);

-- La opción de defensa llega a +28 solo en este premio: las joyas siguen frenando en +16 y el drop no pasa de su máximo.
create temp table nivel_nuevo (nivel int, valor real, valor_id uuid, power_id uuid) on commit drop;
insert into nivel_nuevo values
  (5, 20, gen_random_uuid(), gen_random_uuid()),
  (6, 24, gen_random_uuid(), gen_random_uuid()),
  (7, 28, gen_random_uuid(), gen_random_uuid());

insert into config."PowerUpDefinitionValue" ("Id", "Value", "AggregateType")
select valor_id, valor, 0 from nivel_nuevo;

insert into config."PowerUpDefinition" ("Id", "TargetAttributeId", "BoostId")
select n.power_id, u."TargetAttributeId", n.valor_id
from nivel_nuevo n
cross join (select p."TargetAttributeId" from config."ItemOptionOfLevel" l
            join config."PowerUpDefinition" p on p."Id" = l."PowerUpDefinitionId"
            where l."IncreasableItemOptionId" = '00000088-0002-0000-0000-000000000000' and l."Level" = 4) u;

insert into config."ItemOptionOfLevel" ("Id", "PowerUpDefinitionId", "IncreasableItemOptionId", "Level", "RequiredItemLevel")
select gen_random_uuid(), power_id, '00000088-0002-0000-0000-000000000000', nivel, 0 from nivel_nuevo;

-- El paquete del premio: un producto por set, con su armadura; se distinguen por el número de precio.
insert into config."CashShopPackage" ("Id", "CashShopConfigurationId", "PackageSequence", "Name", "Price", "CoinType", "IsForSale", "IsGiftable", "IsBundle")
select gen_random_uuid(), c."Id", 210, 'Premio 501 resets', 501, ref."CoinType", true, false, false
from config."CashShopConfiguration" c
cross join (select "CoinType" from config."CashShopPackage" where "PackageSequence" = 209) ref;

insert into config."CashShopProduct" ("Id", "ItemDefinitionId", "CashShopPackageId", "ProductSequence", "PriceSequence", "Price", "ItemLevel", "Quantity", "Duration")
select gen_random_uuid(), d."Id", p."Id", 210, 1000 + d."Number", 501, 13, 1, interval '0'
from config."ItemDefinition" d
join config."CashShopPackage" p on p."PackageSequence" = 210
where d."Group" = 8 and d."Number" between 0 and 38;

-- El hito de 501 resets, décimo de la lista, que corresponde al paquete 210.
update config."PlugInConfiguration"
set "CustomConfiguration" = regexp_replace("CustomConfiguration",
      '\}\s*\]\s*\}\s*,\s*"RepeatFrom"',
      '},{"$id":"","Resets":501,"Packages":{"$id":"","$values":[]},"ClassWings":0,"VipDays":0,"ChoosesSet":true}]},"RepeatFrom"')
where "CustomConfiguration" like '%"Milestones"%' and "CustomConfiguration" not like '%"ChoosesSet"%';

do $$
begin
  if (select count(*) from config."CashShopProduct" where "ProductSequence" = 210) <> 39 then
    raise exception 'Faltan sets en el premio de 501 resets';
  end if;
  if not exists (select 1 from config."PlugInConfiguration" where "CustomConfiguration" like '%"ChoosesSet":true%') then
    raise exception 'No se agregó el hito de 501 resets';
  end if;
end $$;

update config."ConfigurationUpdate" set "Version" = 7, "UpdatedAt" = '2026-11-04 15:00:00+00', "InstalledAt" = now() where "Name" = 'MU GKZ: item shop';

commit;
