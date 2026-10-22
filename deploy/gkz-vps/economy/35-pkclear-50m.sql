-- MU GKZ: sacarse el PK con /pkclear cuesta 50.000.000 de zen fijos, sin importar las kills
-- (pedido por Federico el 2026-10-21). Antes eran 10.000.000 por kill.
-- Requiere el server con FixedZenCost (2026-10-21). Toma efecto al reiniciar.

begin;

update config."PlugInConfiguration"
set "CustomConfiguration" = (("CustomConfiguration"::jsonb) || '{"FixedZenCost": 50000000}'::jsonb)::text
where "TypeId" = 'eb97a8f6-f6bd-460a-bcbe-253bf679361a';

select "IsActive", "CustomConfiguration" from config."PlugInConfiguration" where "TypeId" = 'eb97a8f6-f6bd-460a-bcbe-253bf679361a';

commit;
