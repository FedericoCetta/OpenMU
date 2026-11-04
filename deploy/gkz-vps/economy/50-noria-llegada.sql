-- MU GKZ: el viaje a Noria y el regreso al morir llegan a 173,110, en la zona segura. Antes era un área de 186,107 a
-- 192,116, con lugares que no se pueden caminar. Necesita reiniciar el server.
begin;

update config."ExitGate" g
set "X1" = 173, "Y1" = 110, "X2" = 173, "Y2" = 110
from config."GameMapDefinition" m
where m."Id" = g."MapId" and m."Number" = 3 and g."IsSpawnGate" and g."X1" = 186 and g."Y1" = 107;

commit;
