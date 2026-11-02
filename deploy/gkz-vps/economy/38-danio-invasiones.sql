-- MU GKZ: los monstruos de la invasión dorada pegan x20 y aciertan x5 (pedido por Federico el 2026-10-29:
-- "que te den un buen trompazo"; primero x5 y después "x4 de lo actual"). Con los valores originales
-- (120 a 560 de daño) casi no hacían nada.
-- Referencia: los monstruos de endgame de los spots pegan 1.100 a 1.800; Erohim 2.000; Maya y Nightmare 3.000 a 3.500.
-- Pone valores fijos (daño original x20, puntería x5), así que se puede correr de nuevo sin multiplicar dos veces.
-- Toma efecto al reiniciar.

begin;

update config."MonsterAttribute" ma
set "Value" = case ad."Designation"
                when 'Minimum Physical Base Damage' then v.minimo
                when 'Maximum Physical Base Damage' then v.maximo
                else v.ataque
              end
from config."MonsterDefinition" m,
     config."AttributeDefinition" ad,
     (values
        -- número, daño mínimo, daño máximo, puntería (Attack Rate PvM); entre paréntesis el original
        (43, 2400, 2500, 375),     -- Golden Budge Dragon (120-125, 75)
        (78, 2500, 2600, 500),     -- Golden Goblin (125-130, 100)
        (54, 2800, 3000, 1150),    -- Golden Soldier (140-150, 230)
        (53, 3400, 3600, 1325),    -- Golden Titan (170-180, 265)
        (81, 3800, 4000, 1550),   -- Golden Vepar (190-200, 310)
        (83, 6400, 7200, 1925),  -- Golden Wheel (320-360, 385)
        (79, 6000, 7000, 2000),  -- Golden Dragon (300-350, 400)
        (80, 6200, 7200, 2075),  -- Golden Lizard King (310-360, 415)
        (82, 9000, 11200, 2250)   -- Golden Tantallos (450-560, 450)
     ) as v(numero, minimo, maximo, ataque)
where ma."MonsterDefinitionId" = m."Id"
  and ma."AttributeDefinitionId" = ad."Id"
  and ad."Designation" in ('Minimum Physical Base Damage', 'Maximum Physical Base Damage', 'Attack Rate (PvM)')
  and m."Number" = v.numero;

select m."Number", split_part(m."Designation", '||', 1) as monstruo,
       max(case when ad."Designation" = 'Minimum Physical Base Damage' then ma."Value" end) as danio_min,
       max(case when ad."Designation" = 'Maximum Physical Base Damage' then ma."Value" end) as danio_max,
       max(case when ad."Designation" = 'Attack Rate (PvM)' then ma."Value" end) as punteria
from config."MonsterDefinition" m
join config."MonsterAttribute" ma on ma."MonsterDefinitionId" = m."Id"
join config."AttributeDefinition" ad on ad."Id" = ma."AttributeDefinitionId"
where m."Number" in (43, 53, 54, 78, 79, 80, 81, 82, 83)
group by m."Number", m."Designation" order by m."Number";

commit;
