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
                 'Arrastrá un ítem excellent sobre otro igual y del mismo nivel y convertilos en uno nuevo: mismo ítem y nivel, siempre con luck, conserva el skill y sale con 1 a 3 opciones excellent al azar (70 %, 25 % y 5 %). Exclusivo para VIP, 15 GKZ Coins por combinación.'),
                (2, 'Tus resets tienen premio', 'Evento',
                 'Cada hito se reclama una vez por personaje en la tienda (X), categoría Reset. A los 25 resets, las alas de 2da de tu clase; a los 75, joyas y 30 días de VIP; a los 100, las alas de 3ra. Y hay más premios en el camino.'),
                (3, 'Reseteá sin perder tus stats', 'Servidor',
                 'El reset te devuelve al nivel 1 sin tocar tus stats ni tus puntos: se mantienen y se siguen sumando. Reset en el nivel 380 (VIP 360) por 5.000.000 de zen, sin límite y sin volver a la selección de personaje. A los 75 resets, aproximadamente, tu personaje queda full.'),
                (4, 'Volvete VIP', 'Novedad',
                 'Experiencia x500 en lugar de x300, +50 % de zen, más chance de drop, reset desde el nivel 360, 6 baúles en lugar de 2 y combinación de excellent. Se compra en el juego con GKZ Coins, desde el botón VIP de la ventana del personaje.'),
                (5, 'Conquistá el castillo', 'Evento',
                 'Todos los domingos a las 20 h (URU-ARG) se pelea el Castle Siege en Valley of Loren. Formá un clan invencible y volvé a ser el rey.')
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
