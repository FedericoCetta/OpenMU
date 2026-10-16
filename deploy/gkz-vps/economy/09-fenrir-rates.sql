-- MU GKZ: baja el drop de los materiales de Fenrir (Crywolf y Kanturu).
-- Splinter of Armor 5 % -> 4 %, Bless of Guardian 2 % -> 1,5 %. Claw of Beast queda en 0,5 %.
begin;
update config."DropItemGroup" set "Chance" = 0.04 where split_part("Description", '||', 1) = 'Splinter of Armor';
update config."DropItemGroup" set "Chance" = 0.015 where split_part("Description", '||', 1) = 'Bless of Guardian';
select split_part("Description", '||', 1) as grupo, "Chance" from config."DropItemGroup"
where split_part("Description", '||', 1) in ('Splinter of Armor', 'Bless of Guardian', 'Claw of Beast');
commit;
