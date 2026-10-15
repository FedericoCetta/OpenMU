-- MU GKZ: the arrival point of the Arena is the entrance of the ring (the safezone south of it), like in the
-- original game, instead of a corner of the lower part of the map; dying there brings you back to it as well.
\set ON_ERROR_STOP on
begin;

-- The gate of the warp "Arena".
update config."ExitGate" g set "X1" = 61, "Y1" = 133, "X2" = 64, "Y2" = 136
from config."WarpInfo" w
where w."GateId" = g."Id" and w."Index" = 1;

-- The other spawn gates of the Arena are only targets of gates now, not respawn points.
update config."ExitGate" g set "IsSpawnGate" = false
from config."GameMapDefinition" m
where m."Id" = g."MapId" and m."Number" = 6
  and g."Id" <> (select "GateId" from config."WarpInfo" where "Index" = 1);

select g."X1", g."Y1", g."X2", g."Y2", g."IsSpawnGate"
from config."ExitGate" g join config."GameMapDefinition" m on m."Id" = g."MapId" where m."Number" = 6 order by g."IsSpawnGate" desc;
commit;
