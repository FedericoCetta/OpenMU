-- MU GKZ: curva de experiencia más pareja (pedido por Federico, 2026-10-29).
-- Antes: del 1 al 170 se subía volando (2 % del tiempo de un reset) y del 256 al 380 era una pared
-- (94 % del tiempo), por el término extra que la fórmula original suma después del 256.
-- Ahora: hasta el 150 es la curva original x6 (más lenta al principio) y desde ahí cada nivel pide
-- un poco más que el anterior, de forma lineal. Un reset completo pide ~54 % del esfuerzo de antes.
-- El cliente tiene la misma cuenta en GkzExperienceForLevel (ZzzInfomation.cpp): si se cambia acá,
-- hay que cambiarla allá y publicar el cliente.
-- También reacomoda la experiencia de los personajes que ya existen: quedan en el mismo nivel y con
-- el mismo porcentaje de la barra, medido con la curva nueva.
-- Toma efecto al reiniciar.

begin;

create function pg_temp.exp_vieja(l int) returns numeric language sql immutable as $$
    select case when l <= 1 then 0
                when l < 256 then 10::numeric * (l + 8) * (l - 1) * (l - 1)
                else 10::numeric * (l + 8) * (l - 1) * (l - 1) + 1000::numeric * (l - 247) * (l - 256) * (l - 256)
           end
$$;

create function pg_temp.exp_nueva(l int) returns numeric language sql immutable as $$
    select case when l <= 1 then 0
                when l <= 150 then 60::numeric * (l + 8) * (l - 1) * (l - 1)
                else 210465480::numeric + 4129800::numeric * (l - 150) + 7000::numeric * (l - 150) * (l - 149)
           end
$$;

-- Con esta curva, x300 da resets de más o menos lo mismo que la curva vieja a x500.
update config."GameConfiguration"
set "ExperienceRate" = 300,
    "ExperienceFormula" ='if(level <= 1, 0, if(level <= 150, 60 * (level + 8) * (level - 1) * (level - 1), 210465480 + 4129800 * (level - 150) + 7000 * (level - 150) * (level - 149)))';

-- Mismo nivel y mismo porcentaje de la barra; el nivel máximo queda justo en el tope.
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

select c."Name", n.nivel, c."Experience"
from data."Character" c
join (select sa."CharacterId", sa."Value"::int as nivel
      from data."StatAttribute" sa
      join config."AttributeDefinition" ad on ad."Id" = sa."DefinitionId" and ad."Designation" = 'Level') n
  on n."CharacterId" = c."Id"
order by n.nivel desc;

select "ExperienceFormula", "ExperienceRate", "MaximumLevel" from config."GameConfiguration";

commit;
