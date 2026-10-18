-- MU GKZ: lluvia de joyas, ajustes pedidos por Federico el 2026-10-18.
-- - Por horario avisa 3 minutos antes (a las :27) y cae de :30 a :32. Con /lluvia avisa 1 minuto antes.
-- - Los mensajes dicen "en Lorencia" (la fuente ya no existe: es el ring PvP).
-- - Cada joya cae con fuegos artificiales (eso va en el código, no en la config).
-- Requiere el server con ForcedPreStartMessageDelay (2026-10-18). Toma efecto al reiniciar.

begin;

update config."PlugInConfiguration"
set "CustomConfiguration" = jsonb_pretty(
    "CustomConfiguration"::jsonb
    || jsonb_build_object(
        'PreStartMessageDelay', '00:03:00',
        'ForcedPreStartMessageDelay', '00:01:00',
        'StartMessage', '¡Lluvia de joyas en 3 minutos en Lorencia!',
        'ForcedStartMessage', '¡Lluvia de joyas en 1 minuto en Lorencia!',
        'RainStartedMessage', '¡Empezó la lluvia de joyas en Lorencia!',
        'Timetable', jsonb_build_object(
            '$id', '',
            '$values', (select jsonb_agg(to_char(make_time(h, 27, 0), 'HH24:MI:SS') order by h) from generate_series(0, 23) h))))
where "TypeId" = '3b7e9d21-4c58-4a6f-b2e0-7f1d5c8a9e63';

select "CustomConfiguration"::jsonb - 'Jewels' - 'Timetable' as config,
       "CustomConfiguration"::jsonb -> 'Timetable' -> '$values' -> 0 as primer_horario
from config."PlugInConfiguration"
where "TypeId" = '3b7e9d21-4c58-4a6f-b2e0-7f1d5c8a9e63';

commit;
