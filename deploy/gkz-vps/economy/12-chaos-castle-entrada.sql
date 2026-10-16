-- MU GKZ: la entrada a Chaos Castle (Armor of Guardsman) se vende de nuevo y deja de caer.
begin;
delete from config."GameMapDefinitionDropItemGroup" where "DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029';
delete from config."DropItemGroupItemDefinition" where "DropItemGroupId" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029';
delete from config."DropItemGroup" where "Id" = '6c1f0e2b-0000-4a7e-8b25-a0d000000029';
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 231;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 100, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 242;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 2, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 244;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 74, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 245;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 253;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 2, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 255;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 259;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 376;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 377;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 415;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 545;
insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") select gen_random_uuid(), "MerchantStoreId", '00000080-000d-001d-0000-000000000000', 32, 1, 0, false, 0, null, 0 from config."MonsterDefinition" where "Number" = 577;
commit;

-- Chaos Castle solo premia con joyas: se sacan los premios de ítems ancient.
begin;
delete from config."MiniGameReward" r
using config."DropItemGroup" g
where g."Id" = r."ItemRewardId" and split_part(g."Description", '||', 1) like 'Rewarded ancient items for Chaos Castle%';
commit;
