-- MU GKZ: el Castle Siege pasa del sábado al domingo a las 20:00 (hora argentina), pedido por Federico el 2026-10-19.
-- Todas las etapas se corren un día para que el ciclo siga igual:
--   lunes descanso, martes inscripción de guilds, miércoles descanso, jueves entrega de marcas,
--   viernes descanso, sábado aviso, domingo 18:00 preparación, 20:00 batalla, 22:00 fin, 22:05 cierre del ciclo.
-- DayOfWeek: 0 = domingo … 6 = sábado. State: los valores de CastleSiegeState.
-- Toma efecto al reiniciar.

begin;

update config."CastleSiegeStateScheduleEntry" e
set "DayOfWeek" = n.dia, "Hour" = n.hora, "Minute" = n.minuto
from (values
  (0, 1, 0, 0),   -- Idle1: lunes 00:00
  (1, 2, 0, 0),   -- RegisterGuild: martes 00:00
  (2, 3, 0, 0),   -- Idle2: miércoles 00:00
  (3, 4, 0, 0),   -- RegisterMark: jueves 00:00
  (4, 5, 0, 0),   -- Idle3: viernes 00:00
  (5, 6, 0, 0),   -- Notify: sábado 00:00
  (6, 0, 18, 0),  -- Ready: domingo 18:00
  (7, 0, 20, 0),  -- Start: domingo 20:00
  (8, 0, 22, 0),  -- End: domingo 22:00
  (9, 0, 22, 5)   -- EndCycle: domingo 22:05
) as n(etapa, dia, hora, minuto)
where e."State" = n.etapa;

select "State", "DayOfWeek", "Hour", "Minute"
from config."CastleSiegeStateScheduleEntry"
order by "State";

commit;
