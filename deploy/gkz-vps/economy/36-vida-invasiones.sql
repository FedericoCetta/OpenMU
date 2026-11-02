-- MU GKZ: la vida de los monstruos de las invasiones (pedido por Federico).
-- 2026-10-28: x20 la original, porque morían en 2 o 3 golpes en un server x300 con resets.
-- 2026-10-29: los dorados x50 más (x1000 la original): un personaje full tiene que tardar 1 o 2 minutos.
-- Red Dragon, White Wizard y los Ogres siguen en x20.
-- Ninguno de estos monstruos tiene spawns fijos: solo aparecen en las invasiones.
-- Pone valores fijos (no multiplica lo que haya), así que se puede correr de nuevo sin multiplicar dos veces.
-- Toma efecto al reiniciar.

begin;

update config."MonsterAttribute" ma
set "Value" = v.vida
from config."MonsterDefinition" m,
     config."AttributeDefinition" ad,
     (values
        (43, 2500000),    -- Golden Budge Dragon (original 2.500)
        (44, 300000),   -- Red Dragon (15.000)
        (53, 6500000),   -- Golden Titan (6.500)
        (54, 5000000),   -- Golden Soldier (5.000)
        (78, 3200000),    -- Golden Goblin (3.200)
        (79, 22000000),   -- Golden Dragon (22.000)
        (80, 25000000),   -- Golden Lizard King (25.000)
        (81, 10000000),   -- Golden Vepar (10.000)
        (82, 32000000),   -- Golden Tantallos (32.000)
        (83, 15000000),   -- Golden Wheel (15.000)
        (135, 520000),  -- White Wizard (26.000)
        (136, 190000),  -- Destructive Ogre Soldier (9.500)
        (137, 240000)   -- Destructive Ogre Archer (12.000)
     ) as v(numero, vida)
where ma."MonsterDefinitionId" = m."Id"
  and ma."AttributeDefinitionId" = ad."Id"
  and ad."Designation" = 'Maximum Health'
  and m."Number" = v.numero;

select m."Number", split_part(m."Designation", '||', 1) as monstruo, ma."Value" as vida
from config."MonsterDefinition" m
join config."MonsterAttribute" ma on ma."MonsterDefinitionId" = m."Id"
join config."AttributeDefinition" ad on ad."Id" = ma."AttributeDefinitionId" and ad."Designation" = 'Maximum Health'
where m."Number" in (43, 44, 53, 54, 78, 79, 80, 81, 82, 83, 135, 136, 137)
order by m."Number";

commit;
