-- MU GKZ: los ítems de Season 4 no caen. Sets Ashcrow, Eclipse, Iris, Valiant y Glorious (7-11/34-38),
-- y las armas Daybreak, Sword Dancer, Shining Scepter, Albatross Bow y Platina Staff.
begin;
create temp table s4 on commit drop as
select d."Id", split_part(d."Name", '||', 1) as nombre from config."ItemDefinition" d
where (d."Group" between 7 and 11 and d."Number" between 34 and 38)
   or (d."Group", d."Number") in ((0, 24), (0, 25), (2, 15), (4, 22), (5, 13));
update config."ItemDefinition" set "DropsFromMonsters" = false where "Id" in (select "Id" from s4);
delete from config."DropItemGroupItemDefinition" where "ItemDefinitionId" in (select "Id" from s4);
delete from config."ItemDropItemGroupItemDefinition" where "ItemDefinitionId" in (select "Id" from s4);
select count(*) as items_season4, string_agg(nombre, ', ' order by nombre) from s4;
commit;
