-- MU GKZ: tope de 32767 en las estadísticas base (el cliente S3 las muestra mal más arriba).
-- El server no deja repartir puntos por encima del tope y avisa al jugador.
begin;
update config."AttributeDefinition" set "MaximumValue" = 32767
where split_part("Designation", '||', 1) in ('Base Strength', 'Base Agility', 'Base Vitality', 'Base Energy', 'Base Leadership');
select split_part("Designation", '||', 1), "MaximumValue" from config."AttributeDefinition"
where split_part("Designation", '||', 1) in ('Base Strength', 'Base Agility', 'Base Vitality', 'Base Energy', 'Base Leadership');
commit;
