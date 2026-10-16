"""Opens the gates of the Arena pens in the client terrain (EncTerrain7.att) and writes the server TerrainData."""
import base64, os
from terraincmp import KEY, BUX, DATA, client
from arenagates import gates
path=os.path.join(DATA,'World7','EncTerrain7.att')
enc=open(path,'rb').read()
def decrypt(src):
    out=bytearray(len(src)); k=0x5E
    for i,b in enumerate(src):
        out[i]=((b^KEY[i%16])-k)&0xFF; k=(b+0x3D)&0xFF
    for i in range(len(out)): out[i]^=BUX[i%3]
    return out
def encrypt(plain):
    p=bytearray(plain)
    for i in range(len(p)): p[i]^=BUX[i%3]
    out=bytearray(len(p)); k=0x5E
    for i,b in enumerate(p):
        s=((b+k)&0xFF)^KEY[i%16]; out[i]=s; k=(s+0x3D)&0xFF
    return bytes(out)
plain=decrypt(enc)
assert encrypt(plain)==enc, 'el cifrado no da ida y vuelta'
cell=1 if len(plain)-4==65536*2 else 0
assert cell==0
opened=[]
for x,y1,y2 in gates():
    for y in (y1,y2):
        i=4+y*256+x
        plain[i]&=~12&0xFF; opened.append((x,y))
open(path,'wb').write(encrypt(plain))
srv=base64.b64decode(open('terrain/6.b64').read()); head=srv[:len(srv)-65536]; body=bytearray(srv[len(srv)-65536:])
for x,y in opened: body[y*256+x]&=~12&0xFF
newsrv=head+bytes(body)
open('arena-terrain-new.b64','w').write(base64.b64encode(newsrv).decode())
assert bytes(body)==bytes(plain[4:]) or sum(((a&12)!=0)!=((b&12)!=0) for a,b in zip(body,plain[4:]))==0
print('abiertas',len(opened),'celdas; cliente y server iguales')
