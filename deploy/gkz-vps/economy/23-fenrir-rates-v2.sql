-- MU GKZ: materiales de Fenrir a la mitad (con los spots densos de Kanturu caían demasiados).
-- Splinter of Armor 4 % -> 2 %, Bless of Guardian 1,5 % -> 0,75 %. Claw of Beast queda en 0,5 %.
begin;
update config."DropItemGroup" set "Chance" = 0.02 where split_part("Description", '||', 1) = 'Splinter of Armor';
update config."DropItemGroup" set "Chance" = 0.0075 where split_part("Description", '||', 1) = 'Bless of Guardian';
select split_part("Description", '||', 1), "Chance" from config."DropItemGroup"
where split_part("Description", '||', 1) in ('Splinter of Armor', 'Bless of Guardian', 'Claw of Beast');
commit;
