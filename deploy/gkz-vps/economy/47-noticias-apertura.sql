-- MU GKZ: noticias de la apertura (pedido por Federico, 2026-11-02).
-- Después de la del 4/11 (script 45) suma combinación, premios por reset, reset con stats, VIP y Castle Siege.
-- Las dos noticias viejas de la beta dejan de publicarse pero quedan guardadas en el admin.
-- Toma efecto al reiniciar.

begin;

update config."PlugInConfiguration" p
set "CustomConfiguration" = jsonb_pretty(jsonb_set(
        p."CustomConfiguration"::jsonb,
        '{Items,$values}',
        (p."CustomConfiguration"::jsonb #> '{Items,$values,0}')::jsonb
        || (select jsonb_agg(jsonb_build_object(
                '$id', '', 'Title', n.titulo, 'Date', '2026-11-02T00:00:00', 'Tag', n.etiqueta,
                'Text', n.texto, 'IsPublished', true) order by n.orden)
            from (values
                (1, 'Combiná tus ítems', 'Novedad',
                 'Arrastrá un ítem Exc sobre otro igual y del mismo +. Sale el mismo ítem con Luck, mantiene el Skill y trae 1 a 3 opciones Exc (70/25/5 %). Solo VIP, 15 GKZ Coins.'),
                (2, 'Premios por reset', 'Evento',
                 'Se canjean en el shop (X), solapa Reset, una vez por personaje. 25 resets: alas 2. 75 resets: joyas y 30 días de VIP. 100 resets: alas 3. Y más premios en el camino.'),
                (3, 'Reset acumulativo', 'Servidor',
                 'Reset en lvl 380 (VIP lvl 360) por 5kk de zen, sin límite de resets y sin deslogear. Volvés a lvl 1 y mantenés stats y puntos. 5 puntos por lvl (DL 6). Full stats 32767 en aprox. 75 resets.'),
                (4, 'VIP', 'Novedad',
                 'Exp x500 (normal x300), +50 % de zen, más drop, reset en lvl 360, 6 baúles y combinación de Exc. Se compra con GKZ Coins desde el botón VIP de la ventana del personaje.'),
                (5, 'Castle Siege', 'Evento',
                 'Todos los domingos a las 20 h (URU-ARG) en Valley of Loren.')
            ) as n(orden, titulo, etiqueta, texto))
        || (select jsonb_agg(i || '{"IsPublished": false}'::jsonb order by o)
            from jsonb_array_elements(p."CustomConfiguration"::jsonb #> '{Items,$values}') with ordinality as e(i, o)
            where o > 1)))
where p."TypeId" = '8e4c2b7d-5a19-4f3e-9c61-2d7b0a8f4e52'
  and p."CustomConfiguration"::jsonb #>> '{Items,$values,0,Title}' = '¡Abrimos el 4 de noviembre!'
  and p."CustomConfiguration"::jsonb #>> '{Items,$values,1,Title}' <> 'Combiná tus ítems';

select i ->> 'Title' as titulo, i ->> 'Tag' as etiqueta, i ->> 'IsPublished' as publicada, length(i ->> 'Text') as largo
from config."PlugInConfiguration" p,
     jsonb_array_elements(p."CustomConfiguration"::jsonb #> '{Items,$values}') i
where p."TypeId" = '8e4c2b7d-5a19-4f3e-9c61-2d7b0a8f4e52';

commit;
