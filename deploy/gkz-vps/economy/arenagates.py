from terraincmp import client
c=client(6)
w=lambda x,y: (c[y*256+x]&12)!=0
def gates():
    res=[]
    for x in range(1,120):
        for y in range(2,120):
            # pillar block (2 rows) at x+1, then 2 rows of single wall, then pillar block (2 rows)
            if all(w(x,yy) for yy in range(y,y+6)) and w(x+1,y) and w(x+1,y+1) and not w(x+1,y+2) and not w(x+1,y+3) and w(x+1,y+4) and w(x+1,y+5) \
               and not w(x-1,y+2) and not w(x-1,y+3):
                res.append((x,y+2,y+3))
    return res
if __name__=='__main__':
    for g in gates(): print('puerta en x=%d, y=%d-%d'%g)
