"""Arma el SQL de un personaje de prueba de PvP de MU GKZ: clase de 3ª, nivel 400, resets y estadísticas al tope,
el set 380 equipado (+15, luck, opción +16 y 3 excellent) con alas de 3ª, y sets ancient en el baúl de la cuenta.
Lo que el personaje tenía equipado pasa al inventario (o al baúl si no entra); no se borra nada.

Uso: python pjprueba.py <nombre del personaje> <salida.sql> [DW|DK|MG|DL]
Con la clase, el personaje pasa a esa clase (de 3ª); sin ella, se queda con la suya.
Lee los datos del server por ssh. El personaje tiene que estar desconectado cuando se aplica el SQL."""
import json, subprocess, sys, uuid

SSH = ['ssh', '-i', '~/.ssh/mu_gkz_vps'.replace('~', __import__('os').path.expanduser('~')), 'root@187.127.41.30']
GLOBAL = '00000088'

def consulta(sql):
    r = subprocess.run(SSH + ['docker exec -i database psql -U postgres -d openmu -tA'], input=sql, capture_output=True, text=True, encoding='utf-8')
    if r.returncode:
        raise SystemExit(r.stderr)
    return r.stdout.strip()

def json_de(sql):
    texto = consulta(f'select coalesce(json_agg(t), \'[]\') from ({sql}) t;')
    return json.loads(texto)

# Opciones (Id de IncreasableItemOption).
LUCK = '00000088-0001-0000-0000-000000000000'
OPCION_DEFENSA = '00000088-0002-0000-0000-000000000000'
OPCION_FISICA = '00000088-0003-0000-0000-000000000000'
OPCION_MAGIA = '00000088-0004-0000-0000-000000000000'
EXC_DEFENSA = ['00000088-0012-0006-0000-000000000000', '00000088-0012-0004-0000-000000000000', '00000088-0012-0002-0000-000000000000']  # vida, reducción de daño, chance de defensa
EXC_FISICA = ['00000088-0013-0006-0000-000000000000', '00000088-0013-0004-0000-000000000000', '00000088-0013-0003-0000-000000000000']  # daño exc, +daño, velocidad
EXC_MAGIA = ['00000088-0014-0006-0000-000000000000', '00000088-0014-0004-0000-000000000000', '00000088-0014-0003-0000-000000000000']
ALA_IGNORAR = '00000088-0065-c72e-0100-000000000000'

# Por clase de 3ª: número de clase, arma, set 380, alas (con su opción de daño) y sets ancient para el baúl.
CLASES = {
    'DW': dict(clase=3, arma=(5, 12, OPCION_MAGIA, EXC_MAGIA), set=30, alas=(12, 37, '00000088-0060-c025-0200-000000000000'),
               ancient=['00000092-0007-0001-0000-000000000000']),  # Heras
    'DK': dict(clase=7, arma=(0, 22, OPCION_FISICA, EXC_FISICA), set=29, alas=(12, 36, '00000088-0060-c024-0200-000000000000'),
               ancient=['00000092-0001-0001-0000-000000000000', '00000092-0001-0002-0000-000000000000']),  # Hyon, Vicious
    'MG': dict(clase=13, arma=(0, 23, OPCION_FISICA, EXC_FISICA), set=32, alas=(12, 39, '00000088-0060-c027-0200-000000000000'),
               ancient=['00000092-000f-0001-0000-000000000000', '00000092-000f-0002-0000-000000000000'],  # Gaion, Muren
               sin_casco=True),  # Explosion Blade, set Volcano (sin casco: el MG no usa), Wing of Ruin
    'DL': dict(clase=17, arma=(2, 14, OPCION_FISICA, EXC_FISICA), set=33, alas=(12, 40, '00000088-0060-c028-0200-000000000000'),
               ancient=['00000092-001a-0001-0000-000000000000', '00000092-001a-0002-0000-000000000000']),  # Agnis, Broy
}
DURABILIDAD_NIVEL_15 = 47
RESETS = 410
NIVEL = 400


def base(numero):
    return 'DW' if numero <= 3 else 'DK' if numero <= 7 else 'Elfa' if numero <= 11 else 'MG' if numero <= 13 else 'DL'


def main():
    nombre, salida = sys.argv[1], sys.argv[2]
    clase_pedida = sys.argv[3].upper() if len(sys.argv) > 3 else None
    pj = json_de(f'''select c."Id", c."InventoryId", a."Id" cuenta, a."VaultId", cc."Number" clase from data."Character" c join data."Account" a on a."Id"=c."AccountId"
                     join config."CharacterClass" cc on cc."Id"=c."CharacterClassId" where c."Name"='{nombre}' ''')
    if not pj:
        raise SystemExit(f'No existe el personaje {nombre}')
    pj = pj[0]
    # Una cuenta nueva no tiene baúl hasta que lo abre en el juego: se crea acá.
    baul_nuevo = pj['VaultId'] is None
    if baul_nuevo:
        pj['VaultId'] = str(uuid.uuid4())
    # Sin clase pedida, se queda con la suya; con una (DW, DK, MG, DL), el personaje pasa a esa clase.
    tipo = clase_pedida or base(pj['clase'])
    if tipo not in CLASES:
        raise SystemExit(f'La clase {tipo} no está preparada')
    conf = CLASES[tipo]
    defs = {(d['Group'], d['Number']): d for d in json_de('select "Id","Group","Number","Width","Height","Durability","MaximumItemLevel" from config."ItemDefinition"')}
    inv = json_de(f'''select i."Id", i."ItemSlot", d."Width", d."Height" from data."Item" i join config."ItemDefinition" d on d."Id"=i."DefinitionId" where i."ItemStorageId"='{pj["InventoryId"]}' ''')
    baul = [] if baul_nuevo else json_de(f'''select i."Id", i."ItemSlot", d."Width", d."Height" from data."Item" i join config."ItemDefinition" d on d."Id"=i."DefinitionId" where i."ItemStorageId"='{pj["VaultId"]}' ''')
    sets = json_de('select "ItemSetGroupId", "Id", "ItemDefinitionId", "BonusOptionId" from config."ItemOfItemSet" where "AncientSetDiscriminator" > 0')

    def ocupadas(items, base_slot):
        cel = set()
        for i in items:
            s = i['ItemSlot'] - base_slot
            if s >= 0:
                cel |= {(s % 8 + a, s // 8 + b) for a in range(i['Width']) for b in range(i['Height'])}
        return cel

    def lugar(cel, w, h, filas):
        for s in range(8 * filas):
            x, y = s % 8, s // 8
            if x + w > 8 or y + h > filas:
                continue
            c = {(x + a, y + b) for a in range(w) for b in range(h)}
            if not c & cel:
                cel |= c
                return s
        return None

    sql = [f'-- MU GKZ: personaje de prueba de PvP {nombre} ({tipo}).', 'begin;']
    if baul_nuevo:
        sql.append(f'''insert into data."ItemStorage" ("Id", "Money") values ('{pj["VaultId"]}', 0);''')
        sql.append(f'''update data."Account" set "VaultId" = '{pj["VaultId"]}' where "Id" = '{pj["cuenta"]}';''')
    # Clase, nivel, resets, estadísticas y puntos.
    sql.append(f'''update data."Character" set "CharacterClassId" = (select "Id" from config."CharacterClass" where "Number" = {conf["clase"]}), "LevelUpPoints" = 0,
  "Experience" = 10::bigint * ({NIVEL} + 8) * ({NIVEL} - 1) * ({NIVEL} - 1) + 1000::bigint * ({NIVEL} - 247) * ({NIVEL} - 256) * ({NIVEL} - 256) where "Id" = '{pj["Id"]}';''')
    stats = {'Level': NIVEL, 'Resets': RESETS, 'Base Strength': 32767, 'Base Agility': 32767, 'Base Vitality': 32767, 'Base Energy': 32767}
    if tipo == 'DL':
        stats['Base Leadership'] = 32767
    for nombre_stat, valor in stats.items():
        sql.append(f'''delete from data."StatAttribute" where "CharacterId" = '{pj["Id"]}' and "DefinitionId" = (select "Id" from config."AttributeDefinition" where split_part("Designation", '||', 1) = '{nombre_stat}');''')
        sql.append(f'''insert into data."StatAttribute" ("Id", "CharacterId", "DefinitionId", "Value") select '{uuid.uuid4()}', '{pj["Id"]}', "Id", {valor} from config."AttributeDefinition" where split_part("Designation", '||', 1) = '{nombre_stat}';''')

    # Lo equipado en los lugares que se reemplazan (arma, escudo, set y alas) pasa al inventario o al baúl.
    # Al cambiar de clase también se vacía la otra mano (p. ej. el arco o las flechas de una elfa).
    reemplazar = {0, 2, 3, 4, 5, 6, 7} | ({1} if tipo == 'DK' or clase_pedida else set())
    quedan = [i for i in inv if i['ItemSlot'] not in reemplazar]
    cel_inv = ocupadas([i for i in quedan if i['ItemSlot'] >= 12], 12)
    cel_baul = ocupadas(baul, 0)
    for i in inv:
        if i['ItemSlot'] in reemplazar:
            s = lugar(cel_inv, i['Width'], i['Height'], 8)
            if s is not None:
                sql.append(f'''update data."Item" set "ItemSlot" = {12 + s} where "Id" = '{i["Id"]}';''')
            else:
                s = lugar(cel_baul, i['Width'], i['Height'], 15)
                if s is None:
                    raise SystemExit('No hay lugar para lo que estaba equipado')
                sql.append(f'''update data."Item" set "ItemStorageId" = '{pj["VaultId"]}', "ItemSlot" = {s} where "Id" = '{i["Id"]}';''')

    def item(storage, slot, g, n, opciones, skill, ancient=None, extra_dur=15):
        d = defs[(g, n)]
        iid = str(uuid.uuid4())
        nivel = min(15, d['MaximumItemLevel'])
        dur = min(255, d['Durability'] + (DURABILIDAD_NIVEL_15 if nivel == 15 else 0) + extra_dur)
        sql.append(f'''insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience")
  values ('{iid}', '{storage}', '{d["Id"]}', {slot}, {dur}, {nivel}, {str(skill).lower()}, 0, null, 0);''')
        for k, (opcion, nivel) in enumerate(opciones):
            sql.append(f'''insert into data."ItemOptionLink" ("Id","ItemOptionId","ItemId","Level","Index") values ('{uuid.uuid4()}', '{opcion}', '{iid}', {nivel}, 0);''')
        if ancient:
            sql.append(f'''insert into data."ItemItemOfItemSet" ("ItemId","ItemOfItemSetId") values ('{iid}', '{ancient["Id"]}');''')
            if ancient['BonusOptionId']:
                sql.append(f'''insert into data."ItemOptionLink" ("Id","ItemOptionId","ItemId","Level","Index") values ('{uuid.uuid4()}', '{ancient["BonusOptionId"]}', '{iid}', 2, 0);''')

    # Set 380 equipado.
    ag, an, aopc, aexc = conf['arma']
    item(pj['InventoryId'], 0, ag, an, [(LUCK, 0), (aopc, 4)] + [(e, 0) for e in aexc], True)
    for slot, grupo in zip((2, 3, 4, 5, 6), (7, 8, 9, 10, 11)):
        if slot == 2 and conf.get('sin_casco'):
            continue
        item(pj['InventoryId'], slot, grupo, conf['set'], [(LUCK, 0), (OPCION_DEFENSA, 4)] + [(e, 0) for e in EXC_DEFENSA], False)
    wg, wn, wopc = conf['alas']
    item(pj['InventoryId'], 7, wg, wn, [(LUCK, 0), (ALA_IGNORAR, 0), (wopc, 4)], False, extra_dur=0)

    # Sets ancient en el baúl: +15, luck, opción +16 y el bonus ancient +10.
    por_def = {v['Id']: v for v in defs.values()}
    for grupo in conf['ancient']:
        piezas = [s for s in sets if s['ItemSetGroupId'] == grupo]
        # Primero se prueba que entre el set completo, en el baúl o si no en el inventario.
        destino = None
        for storage, cel, filas, base_slot in ((pj['VaultId'], cel_baul, 15, 0), (pj['InventoryId'], cel_inv, 8, 12)):
            prueba = set(cel)
            if all(lugar(prueba, por_def[p['ItemDefinitionId']]['Width'], por_def[p['ItemDefinitionId']]['Height'], filas) is not None for p in piezas):
                destino = (storage, cel, filas, base_slot)
                break
        if destino is None:
            print(f'AVISO: el set ancient {grupo} no entra ni en el baúl ni en el inventario; se saltea')
            continue
        storage, cel, filas, base_slot = destino
        print('set ancient', grupo, 'en el', 'baúl' if base_slot == 0 else 'inventario')
        for pieza in piezas:
            d = por_def[pieza['ItemDefinitionId']]
            s = base_slot + lugar(cel, d['Width'], d['Height'], filas)
            if d['Group'] <= 5:
                opciones = [(LUCK, 0), (OPCION_MAGIA if d['Group'] == 5 else OPCION_FISICA, 4)]
            elif d['Group'] <= 11:
                opciones = [(LUCK, 0), (OPCION_DEFENSA, 4)]
            else:
                opciones = []  # anillos y pendientes
            item(storage, s, d['Group'], d['Number'], opciones, d['Group'] <= 5, ancient=pieza, extra_dur=20)

    sql.append('commit;')
    open(salida, 'w', encoding='utf-8').write('\n'.join(sql) + '\n')
    print(f'{nombre}: {tipo}, SQL en {salida}')


if __name__ == '__main__':
    main()
