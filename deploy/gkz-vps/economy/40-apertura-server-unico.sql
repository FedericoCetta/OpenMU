-- MU GKZ: preparación de la apertura (pedido por Federico, 2026-11-02).
-- Un solo servidor para entrar (antes había 3), tope de 100 jugadores, y el reset deja al personaje
-- en el juego: lo lleva a su mapa de inicio sin pasar por la selección de personaje.
-- Usa el código nuevo de ResetCharacterAction, así que va junto con la imagen nueva del server.
-- Toma efecto al reiniciar.

begin;

delete from config."GameServerDefinition" where "ServerID" <> 0;

update config."GameServerConfiguration" set "MaximumPlayers" = 100;

update config."PlugInConfiguration"
set "CustomConfiguration" = regexp_replace("CustomConfiguration", '("LogOut":\s*)true', '\1false')
where "Id" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';

select "ServerID", "Description" from config."GameServerDefinition";
select "MaximumPlayers" from config."GameServerConfiguration";
select "CustomConfiguration" like '%"LogOut": false%' as reset_sin_seleccion
from config."PlugInConfiguration" where "Id" = '6a9d585d-79d7-4674-b6ea-7e87392fa501';

commit;
