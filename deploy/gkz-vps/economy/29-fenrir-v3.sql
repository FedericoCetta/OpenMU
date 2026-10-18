-- MU GKZ: Fenrir v3 (decidido con Federico el 2026-10-18).
-- Menos drop de materiales, pero las combinaciones fallan menos:
-- - Drop (Kanturu I, Kanturu III, Crywolf): Splinter of Armor 2 % -> 1 %, Bless of Guardian 0,75 % -> 0,4 %,
--   Claw of Beast 0,5 % -> 0,2 %.
-- - Éxito: Fenrir Stage 1 70 % -> 80 %, Stage 2 50 % -> 60 %, Stage 3 30 % -> 40 %.
--   La mejora de color (Stage 4: negro con armas, azul con armaduras) no cambia: depende del valor
--   de los ítems, con un tope de 79 %.
-- La configuración queda en caché en el server: toma efecto al reiniciar.

begin;

update config."DropItemGroup" set "Chance" = 0.01 where split_part("Description", '||', 1) = 'Splinter of Armor';
update config."DropItemGroup" set "Chance" = 0.004 where split_part("Description", '||', 1) = 'Bless of Guardian';
update config."DropItemGroup" set "Chance" = 0.002 where split_part("Description", '||', 1) = 'Claw of Beast';

update config."SimpleCraftingSettings" s
set "SuccessPercent" = case c."Number" when 25 then 80 when 26 then 60 when 27 then 40 end
from config."ItemCrafting" c
where c."SimpleCraftingSettingsId" = s."Id" and c."Number" in (25, 26, 27);

select split_part("Description", '||', 1) as material, "Chance"
from config."DropItemGroup"
where split_part("Description", '||', 1) in ('Splinter of Armor', 'Bless of Guardian', 'Claw of Beast')
order by 1;

select c."Number", split_part(c."Name", '||', 1) as receta, s."SuccessPercent"
from config."ItemCrafting" c join config."SimpleCraftingSettings" s on s."Id" = c."SimpleCraftingSettingsId"
where c."Number" in (25, 26, 27)
order by 1;

commit;
