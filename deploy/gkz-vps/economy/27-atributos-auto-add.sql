-- MU GKZ: los atributos donde cada personaje guarda la configuración del Auto Add del panel de estadísticas
-- (GkzStatPanel.cs): cada cuántos niveles y el % de fuerza, agilidad, vitalidad, energía y comando.
begin;
insert into config."AttributeDefinition" ("Id", "GameConfigurationId", "Designation", "Description")
select v.id::uuid, '00000001-0001-0000-0000-000000000000', v.nombre, v.descripcion
from (values
  ('a3e7c1d2-5b48-4f9a-9c61-0d2e8f7b4a10', 'GKZ auto add: every levels', 'The automatic distribution of the free stat points: every how many levels; 0 is off.'),
  ('a3e7c1d2-5b48-4f9a-9c61-0d2e8f7b4a11', 'GKZ auto add: strength %', 'The percentage of the free stat points which go to strength.'),
  ('a3e7c1d2-5b48-4f9a-9c61-0d2e8f7b4a12', 'GKZ auto add: agility %', 'The percentage of the free stat points which go to agility.'),
  ('a3e7c1d2-5b48-4f9a-9c61-0d2e8f7b4a13', 'GKZ auto add: vitality %', 'The percentage of the free stat points which go to vitality.'),
  ('a3e7c1d2-5b48-4f9a-9c61-0d2e8f7b4a14', 'GKZ auto add: energy %', 'The percentage of the free stat points which go to energy.'),
  ('a3e7c1d2-5b48-4f9a-9c61-0d2e8f7b4a15', 'GKZ auto add: command %', 'The percentage of the free stat points which go to command.')
) v(id, nombre, descripcion)
where not exists (select 1 from config."AttributeDefinition" a where a."Id" = v.id::uuid);
select "Designation" from config."AttributeDefinition" where "Designation" like 'GKZ auto add%' order by 1;
commit;
