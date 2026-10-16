"""Copia el terreno del cliente al server en los mapas indicados y genera el SQL.
Se dejan como están las filas del borde (y 254-255) y las casillas que en el cliente tienen la marca 2."""
import base64, sys
from terraincmp import client, server

def nuevo_terreno(n):
    c = client(n); h, s = server(n)
    celdas = bytearray(s)
    cambios = 0
    for i in range(65536):
        if i // 256 >= 254 or c[i] & 2:
            continue
        if celdas[i] != c[i]:
            celdas[i] = c[i]; cambios += 1
    return h + bytes(celdas), cambios

if __name__ == '__main__':
    salida = sys.argv[1]; mapas = list(map(int, sys.argv[2:]))
    sql = ['-- MU GKZ: el terreno del server pasa a ser igual al del cliente (casillas caminables y zona segura).', 'begin;']
    for n in mapas:
        datos, cambios = nuevo_terreno(n)
        print(f'mapa {n}: {cambios} casillas cambian')
        sql.append(f'update config."GameMapDefinition" set "TerrainData" = decode(\'{datos.hex()}\', \'hex\') where "Number" = {n};')
    sql.append('commit;')
    open(salida, 'w', encoding='utf-8').write('\n'.join(sql) + '\n')
