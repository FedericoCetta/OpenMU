-- MU GKZ: Happy Hour diaria (decidido con Federico el 2026-10-18).
-- - Todos los días a las 20 h de Argentina, durante 1 hora. El server cuenta los horarios en UTC
--   (TimeZoneId vacío), así que en la config va 23:00.
-- - +5 % de experiencia (multiplicador 1,05) y el reset cuesta 500.000 de zen en vez de 5.000.000.
-- - Se anuncia con aviso dorado al empezar y al terminar.
-- Antes había 4 Happy Hours por día (00:05, 06:05, 12:05 y 18:05 UTC) con x1,5.
-- Requiere el server con ResetRequiredMoney (2026-10-18). La config queda en caché: toma efecto al reiniciar.

begin;

update config."PlugInConfiguration"
set "CustomConfiguration" = '{
  "$id": "",
  "ExperienceMultiplier": 1.05,
  "ResetRequiredMoney": 500000,
  "PreStartMessageDelay": "00:00:00",
  "TaskDuration": "01:00:00",
  "StartMessage": "¡Happy Hour hasta las 21 h! +5 % de experiencia y el reset cuesta 500.000 de zen.",
  "EndMessage": "Terminó la Happy Hour. ¡Mañana a las 20 h hay otra!",
  "Timetable": {
    "$id": "",
    "$values": [
      "23:00:00"
    ]
  }
}'
where "TypeId" = '6542e452-9780-45b8-85ae-4036422e9a6e';

select "IsActive", "CustomConfiguration"
from config."PlugInConfiguration"
where "TypeId" = '6542e452-9780-45b8-85ae-4036422e9a6e';

commit;
