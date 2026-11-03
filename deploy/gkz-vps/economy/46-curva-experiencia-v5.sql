-- MU GKZ: quinto ajuste de la curva de experiencia (pedido por Federico, 2026-11-03).
-- Del 200 al 300 se subía demasiado rápido. Hasta el 200 queda igual; desde ahí cada nivel suma un costo
-- extra que crece hasta 1.200.000 en el 300 y sigue fijo en ese valor hasta el final.
-- El tramo 200-300 pide un 30 % más; un reset completo, un 22 % más.
-- El cliente tiene la misma cuenta en GkzExperienceForLevel (ZzzInfomation.cpp).
-- Los personajes quedan en el mismo nivel y con el mismo porcentaje de la barra.
-- Toma efecto al reiniciar. Correr con el server frenado.

begin;

create function pg_temp.exp_vieja(l int) returns numeric language sql immutable as $$
    select case when l <= 1 then 0
                when l <= 20 then 60::numeric * (l + 8) * (l - 1) * (l - 1)
                when l <= 50 then 606480::numeric + 81600::numeric * (l - 20)
                when l <= 150 then 1661900::numeric + 10::numeric * (l + 8) * (l - 1) * (l - 1)
                else 36739480::numeric + 688300::numeric * (l - 150) + 3500::numeric * (l - 150) * (l - 149)
                     + 10::numeric * (l - 150) * (l - 149) * (2 * (l - 150) + 1)
                     + case when l <= 300 then 0
                            else 5000::numeric * (l - 300) * (l - 299) + 50::numeric * (l - 300) * (l - 299) * (2 * (l - 300) + 1)
                       end
           end
$$;

create function pg_temp.exp_nueva(l int) returns numeric language sql immutable as $$
    select pg_temp.exp_vieja(l) + case when l <= 200 then 0
                when l <= 300 then 6000::numeric * (l - 200) * (l - 199)
                else 60600000::numeric + 1200000::numeric * (l - 300)
           end
$$;

update config."GameConfiguration"
set "ExperienceFormula" = 'if(level <= 1, 0, if(level <= 20, 60 * (level + 8) * (level - 1) * (level - 1), if(level <= 50, 606480 + 81600 * (level - 20), if(level <= 150, 1661900 + 10 * (level + 8) * (level - 1) * (level - 1), 36739480 + 688300 * (level - 150) + 3500 * (level - 150) * (level - 149) + 10 * (level - 150) * (level - 149) * (2 * (level - 150) + 1) + if(level <= 300, 0, 5000 * (level - 300) * (level - 299) + 50 * (level - 300) * (level - 299) * (2 * (level - 300) + 1)) + if(level <= 200, 0, if(level <= 300, 6000 * (level - 200) * (level - 199), 60600000 + 1200000 * (level - 300)))))))';

update data."Character" c
set "Experience" = case
        when n.nivel >= g."MaximumLevel" then pg_temp.exp_nueva(n.nivel)
        else pg_temp.exp_nueva(n.nivel) + floor(
            (pg_temp.exp_nueva(n.nivel + 1) - pg_temp.exp_nueva(n.nivel))
            * least(greatest((c."Experience" - pg_temp.exp_vieja(n.nivel))
                             / nullif(pg_temp.exp_vieja(n.nivel + 1) - pg_temp.exp_vieja(n.nivel), 0), 0), 1))
    end
from (select sa."CharacterId", sa."Value"::int as nivel
      from data."StatAttribute" sa
      join config."AttributeDefinition" ad on ad."Id" = sa."DefinitionId" and ad."Designation" = 'Level') n,
     config."GameConfiguration" g
where n."CharacterId" = c."Id";

select l as nivel, pg_temp.exp_vieja(l) as antes, pg_temp.exp_nueva(l) as ahora,
       round(pg_temp.exp_nueva(l) / nullif(pg_temp.exp_vieja(l), 0), 2) as veces
from unnest(array[200, 201, 250, 300, 301, 322, 380, 400]) l;

select c."Name", n.nivel, c."Experience"
from data."Character" c
join (select sa."CharacterId", sa."Value"::int as nivel
      from data."StatAttribute" sa
      join config."AttributeDefinition" ad on ad."Id" = sa."DefinitionId" and ad."Designation" = 'Level') n
  on n."CharacterId" = c."Id"
order by n.nivel desc
limit 5;

commit;
