"""MU GKZ: reset times without Kalima (event maps), and the zen which a normal player gets per reset."""
import csv, sys

KALIMA = {24, 25, 26, 27, 28, 29, 36}
ROWS = [r for r in csv.DictReader(open(sys.argv[1]))
        if r['level'] and r['monster_name'] and float(r['respawn_s'] or 0) <= 60 and int(r['map']) not in KALIMA]


def total_exp(level):
    if level == 0:
        return 0
    value = 10 * (level + 8) * (level - 1) * (level - 1)
    if level >= 256:
        value += 1000 * (level - 247) * (level - 256) * (level - 256)
    return value


def base_exp(monster_level, killer_level):
    temp = (monster_level + 25) * monster_level / 3.0
    if killer_level > monster_level + 10:
        temp *= (monster_level + 10) / killer_level
    if monster_level >= 65:
        temp += (monster_level - 64) * (monster_level // 4)
    return max(temp, 0) * 1.25


def best_spot(level, rate, kill_cap):
    best = None
    for r in ROWS:
        monster_level = float(r['level'])
        per_kill = base_exp(monster_level, level) * rate * float(r['map_exp'])
        kills = min(kill_cap, float(r['quantity']) * 60 / max(float(r['respawn_s']), 1))
        if best is None or per_kill * kills > best[0]:
            best = (per_kill * kills, per_kill, kills, r['map_name'], r['monster_name'], int(monster_level))
    return best


def run(target_level, rate, kill_cap):
    minutes = zen = 0.0
    spots = []
    for level in range(1, target_level):
        per_minute, per_kill, kills, map_name, monster, monster_level = best_spot(level, rate, kill_cap)
        need = total_exp(level + 1) - total_exp(level)
        level_minutes = need / per_minute
        minutes += level_minutes
        # Half of the kills drop money: the experience of the kill plus 7 (DefaultDropGenerator).
        zen += level_minutes * kills * 0.5 * (per_kill + 7)
        if not spots or spots[-1][1] != (map_name, monster):
            spots.append((level, (map_name, monster), monster_level))
    return minutes, zen, spots


for kill_cap in (15, 30, 60):
    n_min, n_zen, _ = run(380, 300, kill_cap)
    v_min, v_zen, _ = run(360, 500, kill_cap)
    print(f"{kill_cap:>2} kills/min: normal {n_min:5.0f} min, zen {n_zen / 1e6:8.0f}M | VIP {v_min:5.0f} min, zen {v_zen / 1e6:8.0f}M")

_, zen30, spots = run(380, 300, 30)
print(f"\nGlobal zen rate for 6M per reset (normal, 30 kills/min): {6e6 / zen30:.5f}")
print("Spot path (normal, 30 kills/min):")
for level, (map_name, monster), monster_level in spots:
    print(f"  from level {level:>3}: {map_name} - {monster} (level {monster_level})")
