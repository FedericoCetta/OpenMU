-- MU GKZ: segundo ajuste de la curva de experiencia (pedido por Federico, 2026-11-02).
-- Probando la del script 39: del 1 al 20 bien, del 20 al 150 muy lento, del 150 al 300 lento y del 300
-- al 350 rápido. Ahora: del 1 al 20 igual; hasta el 35 cada nivel cuesta lo mismo; hasta el 150 la curva
-- original x2 (antes x6); desde ahí cada nivel pide un poco más que el anterior, con menos costo en el
-- medio y más al final. Un reset pide ~85 % de lo que pedía la del script 39.
-- El cliente tiene la misma cuenta en GkzExperienceForLevel (ZzzInfomation.cpp).
-- Los personajes quedan en el mismo nivel y con el mismo porcentaje de la barra.
-- Toma efecto al reiniciar. Correr con el server frenado.

begin;

create function pg_temp.exp_vieja(l int) returns numeric language sql immutable as $$
    select case when l <= 1 then 0
                when l <= 150 then 60::numeric * (l + 8) * (l - 1) * (l - 1)
                else 210465480::numeric + 4129800::numeric * (l - 150) + 7000::numeric * (l - 150) * (l - 149)
           end
$$;

create function pg_temp.exp_nueva(l int) returns numeric language sql immutable as $$
    select case when l <= 1 then 0
                when l <= 20 then 60::numeric * (l + 8) * (l - 1) * (l - 1)
                when l <= 35 then 606480::numeric + 81600::numeric * (l - 20)
                when l <= 150 then 836320::numeric + 20::numeric * (l + 8) * (l - 1) * (l - 1)
                else 70991480::numeric + 1376600::numeric * (l - 150) + 7000::numeric * (l - 150) * (l - 149)
                     + 20::numeric * (l - 150) * (l - 149) * (2 * (l - 150) + 1)
           end
$$;

update config."GameConfiguration"
set "ExperienceFormula" = 'if(level <= 1, 0, if(level <= 20, 60 * (level + 8) * (level - 1) * (level - 1), if(level <= 35, 606480 + 81600 * (level - 20), if(level <= 150, 836320 + 20 * (level + 8) * (level - 1) * (level - 1), 70991480 + 1376600 * (level - 150) + 7000 * (level - 150) * (level - 149) + 20 * (level - 150) * (level - 149) * (2 * (level - 150) + 1)))))';

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

commit;
