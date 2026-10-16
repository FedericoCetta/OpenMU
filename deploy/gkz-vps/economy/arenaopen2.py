"""Abre las puertas de los dos corrales de arriba de Arena (pared de abajo), en el cliente y en el server."""
import base64, os
from terraincmp import KEY, BUX, DATA
CELDAS = [(16, 86), (17, 86), (29, 86), (30, 86)]
path = os.path.join(DATA, 'World7', 'EncTerrain7.att')
def descifrar(src):
    out = bytearray(len(src)); k = 0x5E
    for i, b in enumerate(src):
        out[i] = ((b ^ KEY[i % 16]) - k) & 0xFF; k = (b + 0x3D) & 0xFF
    for i in range(len(out)): out[i] ^= BUX[i % 3]
    return out
def cifrar(plano):
    p = bytearray(plano)
    for i in range(len(p)): p[i] ^= BUX[i % 3]
    out = bytearray(len(p)); k = 0x5E
    for i, b in enumerate(p):
        s = ((b + k) & 0xFF) ^ KEY[i % 16]; out[i] = s; k = (s + 0x3D) & 0xFF
    return bytes(out)
enc = open(path, 'rb').read(); plano = descifrar(enc)
assert cifrar(plano) == enc
for x, y in CELDAS: plano[4 + y * 256 + x] &= ~12 & 0xFF
open(path, 'wb').write(cifrar(plano))
srv = base64.b64decode(open('arena-terrain-new.b64').read()); cab = srv[:len(srv) - 65536]; cuerpo = bytearray(srv[len(srv) - 65536:])
for x, y in CELDAS: cuerpo[y * 256 + x] &= ~12 & 0xFF
assert all(((a & 12) != 0) == ((b & 12) != 0) for a, b in zip(cuerpo, plano[4:]))
open('arena-terrain-new.b64', 'w').write(base64.b64encode(cab + bytes(cuerpo)).decode())
print('abiertas', len(CELDAS), 'celdas; cliente y server iguales')
