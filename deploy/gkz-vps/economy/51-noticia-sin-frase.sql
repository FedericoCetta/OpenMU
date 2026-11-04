-- MU GKZ: saca una frase de la noticia de apertura. Se ve en la web después de reiniciar el server.
begin;

update config."PlugInConfiguration"
set "CustomConfiguration" = replace("CustomConfiguration",
  'Reviví lo mejor del MU. El miércoles 4 de noviembre a las 19 h (URU-ARG) abre MU GKZ, Season 3 como se jugaba.',
  'El miércoles 4 de noviembre a las 19 h (URU-ARG) abre MU GKZ, Season 3 old school.')
where "CustomConfiguration" like '%Season 3 como se jugaba%';

commit;
