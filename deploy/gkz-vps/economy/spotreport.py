import csv, json
KAL={24,25,26,27,28,29,36}
ACCESS={0:1,1:30,2:20,3:1,4:50,7:70,8:140,10:170,33:150,34:280,37:160,38:230,31:9999,41:9999,42:9999}
rows=[r for r in csv.DictReader(open('spawns.csv')) if r['level'] and int(r['map']) not in KAL and float(r['respawn_s'])<=60]
def total_exp(l):
    if l==0: return 0
    v=10*(l+8)*(l-1)*(l-1)
    if l>=256: v+=1000*(l-247)*(l-256)*(l-256)
    return v
def base_exp(m,k):
    t=(m+25)*m/3.0
    if k>m+10: t*=(m+10)/k
    if m>=65: t+=(m-64)*(m//4)
    return max(t,0)*1.25
RATE,ZEN,CAP=300,0.0046,30
def map_rate(mapn,level):
    best=None
    for r in rows:
        if int(r['map'])!=mapn: continue
        ml=float(r['level']); pk=base_exp(ml,level)*RATE*float(r['map_exp'])
        kills=min(CAP,float(r['quantity'])*60/max(float(r['respawn_s']),1))
        if best is None or pk*kills>best[0]: best=(pk*kills,pk,kills,r['monster_name'],int(ml))
    return best
maps=sorted({(int(r['map']),r['map_name']) for r in rows})
levels=[30,80,120,160,200,250,300,350,379]
print('exp/min (millones) del mejor monstruo de cada mapa, a 30 kills/min, exp x300')
print(f"{'mapa':<20}{'acceso':>7}"+''.join(f"{l:>8}" for l in levels))
for n,name in maps:
    acc=ACCESS.get(n,1)
    print(f"{name:<20}{('-' if acc>=9999 else acc):>7}"+''.join(f"{map_rate(n,l)[0]/1e6:8.2f}" for l in levels))
print()
# progression: best accessible map per level, time per band
band=[];t=0;zen=0
for l in range(1,380):
    opts=[(map_rate(n,l),n,name) for n,name in maps if ACCESS.get(n,1)<=l]
    (pm,pk,kills,mon,ml),n,name=max(opts,key=lambda o:o[0][0])
    need=total_exp(l+1)-total_exp(l); mins=need/pm; t+=mins; zen+=mins*kills*0.5*(pk+7)*ZEN
    if not band or band[-1][1]!=name: band.append([l,name,mon,ml,0.0,0.0])
    band[-1][4]+=mins; band[-1][5]+=mins*kills*0.5*(pk+7)*ZEN
print(f"Reset completo (1->380, sin VIP): {t:.0f} min, zen {zen/1e6:.1f}M")
for l,name,mon,ml,m,z in band: print(f"  desde nivel {l:>3}: {name:<18} {mon:<20} (L{ml})  {m:6.1f} min  zen {z/1e6:5.2f}M")
print()
print('Capacidad a nivel 330: monstruos que dan >=80% de la exp del mejor del mapa')
for n,name in maps:
    if ACCESS.get(n,1)>=9999 or ACCESS.get(n,1)<140: continue
    rs=[r for r in rows if int(r['map'])==n]
    best=max(base_exp(float(r['level']),330) for r in rs)
    good=[r for r in rs if base_exp(float(r['level']),330)>=0.8*best]
    kpm=sum(float(r['quantity'])*60/max(float(r['respawn_s']),1) for r in good)
    print(f"  {name:<18} {sum(int(r['quantity']) for r in good):>4} monstruos, {kpm:6.0f} kills/min -> {kpm/30:4.1f} jugadores a 30 kills/min  ({', '.join(r['monster_name'] for r in good)})")
