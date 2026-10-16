-- MU GKZ: Arena desde nivel 150 (MG y DL 120) y Crywolf desde 220 (MG y DL 200).
-- Lo de MG y DL está en GkzWarpLevels.cs (server) y NewUIMoveCommandWindow.cpp (cliente).
begin;
update config."WarpInfo" set "LevelRequirement" = 150 where "Index" = 1;
update config."WarpInfo" set "LevelRequirement" = 220 where "Index" = 24;
update config."EnterGate" set "LevelRequirement" = 220 where "Number" = 117;
select 'warp', "Index", split_part("Name", '||', 1), "LevelRequirement" from config."WarpInfo" where "Index" in (1, 24)
union all select 'puerta', "Number", '', "LevelRequirement" from config."EnterGate" where "Number" = 117;
commit;
