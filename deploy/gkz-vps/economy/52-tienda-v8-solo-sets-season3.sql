-- MU GKZ: tienda v8, igual que GkzCashShop.json (scriptYearId 8). El premio de 501 resets deja de ofrecer los sets
-- posteriores a Season 3, que no caen en este server: Ashcrow, Eclipse, Iris, Valiant y Glorious.
-- Necesita el cliente con el script 512.2026.008 y reiniciar el server.
begin;

update config."CashShopConfiguration" set "ScriptYearId" = 8;

delete from config."CashShopProduct" where "ProductSequence" = 210 and "PriceSequence" between 1034 and 1038;

do $$
begin
  if (select count(*) from config."CashShopProduct" where "ProductSequence" = 210) <> 34 then
    raise exception 'El premio de 501 resets tiene que quedar con 34 sets';
  end if;
  if exists (select 1 from data."CashShopStorageItem" where "ProductSequence" = 210 and "PriceSequence" between 1034 and 1038) then
    raise exception 'Alguien ya canjeó uno de estos sets: hay que resolverlo antes';
  end if;
end $$;

update config."ConfigurationUpdate" set "Version" = 8, "UpdatedAt" = '2026-11-04 16:00:00+00', "InstalledAt" = now() where "Name" = 'MU GKZ: item shop';

commit;
