-- MU GKZ: drop de joyas al doble (decidido con Federico el 2026-10-18).
-- - Bless 0,22 % -> 0,44 %, Soul 0,18 % -> 0,36 %, Chaos 0,18 % -> 0,36 %, Creation 0,03 % -> 0,06 %.
-- - Life (0,06 %) y Guardian (0,04 %) quedan igual.
-- La configuración queda en caché en el server: toma efecto al reiniciar.

begin;

update config."DropItemGroup"
set "Chance" = case "Description"
    when 'GKZ jewel: Jewel of Bless' then 0.0044
    when 'GKZ jewel: Jewel of Soul' then 0.0036
    when 'GKZ jewel: Jewel of Chaos' then 0.0036
    when 'GKZ jewel: Jewel of Creation' then 0.0006
end
where "Description" in (
    'GKZ jewel: Jewel of Bless', 'GKZ jewel: Jewel of Soul',
    'GKZ jewel: Jewel of Chaos', 'GKZ jewel: Jewel of Creation');

select "Description" as joya, "Chance"
from config."DropItemGroup"
where "Description" like 'GKZ jewel: %'
order by 1;

commit;
