"""Analiza para qué lado difiere el terreno del cliente y el del server, y en qué zonas (grupos de celdas)."""
import sys
from terraincmp import client, server

def grupos(celdas):
    celdas = set(celdas); res = []
    while celdas:
        pila = [celdas.pop()]; g = []
        while pila:
            x, y = pila.pop(); g.append((x, y))
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    v = (x + dx, y + dy)
                    if v in celdas: celdas.remove(v); pila.append(v)
        xs = [c[0] for c in g]; ys = [c[1] for c in g]
        res.append((len(g), min(xs), min(ys), max(xs), max(ys)))
    return sorted(res, reverse=True)

for n in map(int, sys.argv[1:]):
    c = client(n); _, s = server(n)
    # El índice es y*256+x en los dos archivos.
    tipos = {
        'server bloquea, cliente camina': lambda i: (s[i] & 12) and not (c[i] & 12),
        'server camina, cliente bloquea': lambda i: not (s[i] & 12) and (c[i] & 12),
        'segura solo en server': lambda i: (s[i] & 1) and not (c[i] & 1),
        'segura solo en cliente': lambda i: not (s[i] & 1) and (c[i] & 1),
    }
    print(f'== mapa {n}')
    for nombre, f in tipos.items():
        cel = [(i % 256, i // 256) for i in range(65536) if f(i)]
        if cel:
            gs = grupos(cel)
            print(f'  {nombre}: {len(cel)} celdas en {len(gs)} zonas; mayores:', '; '.join(f'{g[0]} en x{g[1]}-{g[3]} y{g[2]}-{g[4]}' for g in gs[:4]))
