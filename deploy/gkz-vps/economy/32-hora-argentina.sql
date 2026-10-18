-- MU GKZ: el server cuenta los horarios en hora de Argentina (UTC-3), pedido por Federico el 2026-10-18.
-- - TimeZoneId: todos los horarios de los eventos (y Castle Siege) se leen en hora argentina.
--   Los números de los horarios no cambian: un evento a las 20:00 ahora es a las 20 h de Argentina.
-- - Happy Hour: vuelve a las 20:00. El aviso no dice la hora de fin porque se puede arrancar a mano con /happyhour.
-- Requiere la imagen con tzdata (2026-10-18). Toma efecto al reiniciar.

begin;

update config."SystemConfiguration" set "TimeZoneId" = 'America/Argentina/Buenos_Aires';

update config."PlugInConfiguration"
set "CustomConfiguration" = '{
  "$id": "",
  "ExperienceMultiplier": 1.05,
  "ResetRequiredMoney": 500000,
  "PreStartMessageDelay": "00:00:00",
  "TaskDuration": "01:00:00",
  "StartMessage": "¡Happy Hour durante 1 hora! +5 % de experiencia y el reset cuesta 500.000 de zen.",
  "EndMessage": "Terminó la Happy Hour. ¡Todos los días a las 20 h hay otra!",
  "Timetable": {
    "$id": "",
    "$values": [
      "20:00:00"
    ]
  }
}'
where "TypeId" = '6542e452-9780-45b8-85ae-4036422e9a6e';

select "TimeZoneId" from config."SystemConfiguration";

commit;
