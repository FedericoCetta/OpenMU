-- MU GKZ: balance del mago y orden de los warps.
-- 1) El multiplicador de skills (DK 2 + Energía×0,001, DL 2 + Energía×0,0005) con tope 5: con 32767 de energía
--    llegaba a ×34,8 y ×18,4, y el mago queda en ×1. La velocidad de magia con tope 400 (antes 200): la animación
--    de magia del cliente suma la mitad que la de ataque, así el mago se mueve tan rápido como el DK.
-- 2) Warps escalonados: Arena 150, Aida 160, Kanturu 1-3 170, Icarus 180, Crywolf 220, Kanturu Relics 230.
--    MG y DL piden el 80 % (GkzWarpLevels.cs), salvo Crywolf 200. Las puertas a esos mapas piden lo mismo.
begin;
update config."AttributeDefinition" set "MaximumValue" = 5 where split_part("Designation", '||', 1) = 'Skill Damage Multiplier';
update config."AttributeDefinition" set "MaximumValue" = 400 where split_part("Designation", '||', 1) = 'Magic Speed';
update config."WarpInfo" set "LevelRequirement" = 160 where "Index" in (25, 27);
update config."WarpInfo" set "LevelRequirement" = 170 where "Index" in (28, 29, 45);
update config."WarpInfo" set "LevelRequirement" = 180 where "Index" = 23;
update config."EnterGate" set "LevelRequirement" = 180 where "Number" = 62;
update config."EnterGate" set "LevelRequirement" = 160 where "Number" = 120;
update config."EnterGate" set "LevelRequirement" = 170 where "Number" in (125, 131);
update config."EnterGate" set "LevelRequirement" = 230 where "Number" = 129;
select split_part("Designation", '||', 1), "MaximumValue" from config."AttributeDefinition" where split_part("Designation", '||', 1) in ('Skill Damage Multiplier', 'Magic Speed');
select 'warp', "Index", split_part("Name", '||', 1), "LevelRequirement" from config."WarpInfo" where "Index" in (1, 23, 24, 25, 27, 28, 29, 30, 45) order by 4;
commit;
