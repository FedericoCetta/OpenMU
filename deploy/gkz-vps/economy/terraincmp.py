"""Compares the walkable/safezone flags of the client terrain (EncTerrainN.att) with the server's TerrainData."""
import base64, os, sys
KEY=bytes([0xD1,0x73,0x52,0xF6,0xD2,0x9A,0xCB,0x27,0x3E,0xAF,0x59,0x31,0x37,0xB3,0xE7,0xA2]); BUX=(0xFC,0xCF,0xAB)
DATA=r'C:\dev\mu-server\MuMain\src\bin\Data'
def client(n):
    p=os.path.join(DATA,f'World{n+1}',f'EncTerrain{n+1}.att')
    if not os.path.exists(p): return None
    src=open(p,'rb').read(); out=bytearray(len(src)); k=0x5E
    for i,b in enumerate(src):
        out[i]=((b^KEY[i%16])-k)&0xFF; k=(b+0x3D)&0xFF
    for i in range(len(out)): out[i]^=BUX[i%3]
    body=out[4:]
    if len(body)==65536*2: body=bytes(body[i*2] for i in range(65536))
    return bytes(body)
def server(n):
    p=f'terrain/{n}.b64'
    if not os.path.exists(p) or os.path.getsize(p)==0: return None,None
    d=base64.b64decode(open(p).read()); return d[:len(d)-65536], d[len(d)-65536:]
if __name__=='__main__':
    for n in map(int,open('mapnums.txt').read().split()):
        c=client(n); h,s=server(n)
        if c is None or s is None: print(n,'sin datos', c is None, s is None); continue
        walk=sum(((c[i]&12)!=0)!=((s[i]&12)!=0) for i in range(65536))
        safe=sum(((c[i]&1)!=0)!=((s[i]&1)!=0) for i in range(65536))
        print(f'mapa {n:>3}: caminable distinto {walk:>5}, zona segura distinta {safe:>5}')
