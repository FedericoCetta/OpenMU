-- MU GKZ: opciones de los ítems que caen (decidido con Federico el 2026-10-18).
-- - Luck: 25 % -> 40 %.
-- - Opción adicional (+4/+8/+12 de daño, defensa o defense rate): 25 % -> 35 %.
-- - Excellent: la 1ª opción sigue siendo segura; cada opción extra sale con 10 % (antes 0,1 %)
--   y el máximo del drop pasa de 2 a 3. Queda ~10 % con 2 opciones y ~1 % con 3.
-- La configuración queda en caché en el server: toma efecto al reiniciar.

begin;

update config."ItemOptionDefinition"
set "AddChance" = 0.4
where split_part("Name", '||', 1) = 'Luck';

update config."ItemOptionDefinition"
set "AddChance" = 0.35
where split_part("Name", '||', 1) in (
    'Base Defense Option',
    'Defense Rate (PvM) Option',
    'Physical Base Damage (min and max) Option',
    'Wizardry Base Damage (min and max) Option',
    'Curse Base Damage (min and max) Option',
    'Base Damage Bonus (physical and wizardry, min and max) Option');

update config."ItemOptionDefinition"
set "AddChance" = 0.1, "MaximumOptionsPerItem" = 3
where split_part("Name", '||', 1) in (
    'Excellent Defense Options',
    'Excellent Physical Attack Options',
    'Excellent Wizardry Attack Options');

select split_part("Name", '||', 1) as opcion, "AddChance", "MaximumOptionsPerItem"
from config."ItemOptionDefinition"
where split_part("Name", '||', 1) in (
    'Luck', 'Base Defense Option', 'Defense Rate (PvM) Option',
    'Physical Base Damage (min and max) Option', 'Wizardry Base Damage (min and max) Option',
    'Curse Base Damage (min and max) Option', 'Base Damage Bonus (physical and wizardry, min and max) Option',
    'Excellent Defense Options', 'Excellent Physical Attack Options', 'Excellent Wizardry Attack Options')
order by 1;

commit;
