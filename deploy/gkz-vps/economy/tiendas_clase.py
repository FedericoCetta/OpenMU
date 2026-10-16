# Genera el SQL de las tiendas de clase v2: sets +6 con suerte y las skills base de cada clase.
import uuid, sys

S = sys.argv[1]
SALIDA = sys.argv[2]
LUCK = '00000088-0001-0000-0000-000000000000'

defs = {}
for l in open(f'{S}/defs.txt', encoding='utf-8'):
    p = l.rstrip('\n').split('|')
    if len(p) < 6:
        continue
    defs[(int(p[0]), int(p[1]))] = dict(w=int(p[2]), h=int(p[3]), dur=float(p[4]), name=p[5])

tienda = {}
for l in open(f'{S}/store_items.txt', encoding='utf-8'):
    p = l.rstrip('\n').split('|')
    if len(p) < 8:
        continue
    tienda.setdefault(int(p[0]), []).append(dict(id=p[1], slot=int(p[2]), g=int(p[3]), n=int(p[4]), w=int(p[5]), h=int(p[6]), name=p[7]))

ARMADURA = range(7, 12)


def existentes(npc, pred):
    return [dict(i, tipo='mantener') for i in tienda[npc] if pred(i)]


def sets(numeros, con_casco=True):
    # Cada set va en su propia columna (de 2 casillas de ancho), de arriba hacia abajo.
    res = []
    for k, n in enumerate(numeros):
        for g in ARMADURA:
            if g == 7 and not con_casco:
                continue
            d = defs[(g, n)]
            res.append(dict(tipo='nuevo', g=g, n=n, w=d['w'], h=d['h'], dur=d['dur'], lvl=6, luck=True, name=d['name'], col=k))
    return res


def skills(lista):
    return [dict(tipo='nuevo', g=g, n=n, w=defs[(g, n)]['w'], h=defs[(g, n)]['h'], dur=1, lvl=0, luck=False, name=defs[(g, n)]['name']) for g, n in lista]


def es(i, *pares):
    return (i['g'], i['n']) in pares


consumible = lambda i: i['g'] in (13, 14) or (i['g'] == 4 and i['n'] in (7, 15))
ORBES_DK = [(12, 7), (12, 13)]
SCROLLS_DW = [(15, 3), (15, 10), (15, 2), (15, 1), (15, 5), (15, 6), (15, 0), (15, 4), (15, 7)]
SCROLLS_MG = [(15, 3), (15, 10), (15, 2), (15, 1), (15, 6), (15, 0), (15, 4), (15, 7)]

plan = {
    # Hanzo (DK): Leather, Bronze, Scale y Plate +6; las armas que dejaste; Twisting Slash e Impale.
    251: sets([5, 0, 6, 9]) + existentes(251, lambda i: i['g'] < 7) + skills(ORBES_DK),
    # Pasi (DW): Pad, Bone, Sphinx y Legendary +6; báculos (sin el Thunder Staff, que no entra); pergaminos base (suma Flame y Twister).
    254: existentes(254, lambda i: i['g'] == 15) + skills([(15, 4), (15, 7)])
         + sets([2, 4, 7, 3]) + existentes(254, lambda i: i['g'] == 5 and i['n'] != 3),
    # Lala (Elfa): pociones, orbes y flechas; Vine, Silk, Wind y Spirit +6.
    242: existentes(242, lambda i: consumible(i) or i['g'] == 12) + sets([10, 11, 12, 13]),
    # Izabel (MG, Devias): pociones; sets de DK sin casco +6; báculos; pergaminos y orbes que usa el MG.
    245: existentes(245, consumible) + sets([5, 0, 6, 9], con_casco=False) + existentes(245, lambda i: i['g'] == 5)
         + skills(SCROLLS_MG + ORBES_DK),
    # Harold (DL, Lorencia 183,137): cetros y escudo; Light Plate, Adamantine y Dark Steel +6.
    250: sets([25, 26, 27]) + existentes(250, lambda i: i['g'] < 7),
}

sql = ['-- MU GKZ: tiendas de clase v2. Sets +6 con suerte y las skills base de cada clase.', 'begin;']
for npc, items in plan.items():
    store = f'(select "MerchantStoreId" from config."MonsterDefinition" where "Number" = {npc})'
    quedan = [i['id'] for i in items if i['tipo'] in ('mantener', 'mover')]
    lista = ', '.join(f"'{x}'" for x in quedan) or "'00000000-0000-0000-0000-000000000000'"
    sql.append(f'delete from data."Item" where "ItemStorageId" = {store} and "Id" not in ({lista});')
    def acomodar(orden):
        ocup, pos = set(), []
        for i in [i for i in orden if 'col' in i]:
            x = 2 * i['col']
            for y in range(15 - i['h'] + 1):
                c = {(x + a, y + b) for a in range(i['w']) for b in range(i['h'])}
                if not c & ocup:
                    ocup |= c
                    pos.append((i, x + 8 * y))
                    break
            else:
                return None, ocup
        for i in [i for i in orden if 'col' not in i]:
            for s in range(120):
                x, y = s % 8, s // 8
                if x + i['w'] > 8 or y + i['h'] > 15:
                    continue
                c = {(x + a, y + b) for a in range(i['w']) for b in range(i['h'])}
                if not c & ocup:
                    ocup |= c
                    pos.append((i, s))
                    break
            else:
                return None, ocup
        return pos, ocup

    # Los sets primero en sus columnas; el resto en el orden pensado o, si no entra, lo más alto y ancho primero.
    pos, ocup = acomodar(items)
    if pos is None:
        pos, ocup = acomodar(sorted(items, key=lambda i: (-i['h'], -i['w'])))
    if pos is None:
        raise SystemExit(f'No entra todo en la tienda {npc}')
    resumen = []
    for i, s in pos:
        resumen.append(f'{s}:{i["name"]}')
        if i['tipo'] == 'nuevo':
            nid = str(uuid.uuid4())
            sql.append(f'insert into data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","StorePrice","PetExperience") '
                       f'select \'{nid}\', {store}, d."Id", {s}, {i["dur"]}, {i["lvl"]}, false, 0, null, 0 from config."ItemDefinition" d where d."Group" = {i["g"]} and d."Number" = {i["n"]};')
            if i['luck']:
                sql.append(f'insert into data."ItemOptionLink" ("Id","ItemOptionId","ItemId","Level","Index") values (\'{uuid.uuid4()}\', \'{LUCK}\', \'{nid}\', 0, 0);')
        else:
            sql.append(f'update data."Item" set "ItemStorageId" = {store}, "ItemSlot" = {s} where "Id" = \'{i["id"]}\';')
    print(npc, len(items), 'ítems,', len(ocup), 'de 120 casillas')
sql.append('commit;')
open(SALIDA, 'w', encoding='utf-8').write('\n'.join(sql) + '\n')
