-- MU GKZ: noticia de la apertura del 4/11 (pedido por Federico, 2026-11-02).
-- Queda primera en la web y en el launcher; las demás noticias no se tocan.
-- Toma efecto al reiniciar. Sin reiniciar se puede publicar desde la página Noticias del admin.

begin;

update config."PlugInConfiguration"
set "CustomConfiguration" = jsonb_pretty(jsonb_set(
        "CustomConfiguration"::jsonb,
        '{Items,$values}',
        jsonb_build_array(jsonb_build_object(
            '$id', '',
            'Title', '¡Abrimos el 4 de noviembre!',
            'Date', '2026-11-02T00:00:00',
            'Tag', 'Evento',
            'Text', 'Reviví lo mejor del MU. El miércoles 4 de noviembre a las 19 h (URU-ARG) abre MU GKZ, Season 3 como se jugaba. Creá tu cuenta desde ahora, juntá a los tuyos y formá un clan invencible: el ranking arranca de cero para todos. Jugá con el launcher o desde el navegador de tu PC, sin instalar nada.',
            'IsPublished', true))
        || ("CustomConfiguration"::jsonb #> '{Items,$values}')))
where "TypeId" = '8e4c2b7d-5a19-4f3e-9c61-2d7b0a8f4e52'
  and "CustomConfiguration"::jsonb #>> '{Items,$values,0,Title}' <> '¡Abrimos el 4 de noviembre!';

select i ->> 'Title' as titulo, i ->> 'Tag' as etiqueta, i ->> 'IsPublished' as publicada
from config."PlugInConfiguration" p,
     jsonb_array_elements(p."CustomConfiguration"::jsonb #> '{Items,$values}') i
where p."TypeId" = '8e4c2b7d-5a19-4f3e-9c61-2d7b0a8f4e52';

commit;
