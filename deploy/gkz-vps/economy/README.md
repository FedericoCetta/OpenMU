# MU GKZ economy

The balance of the server, applied to the database of the VPS in this order (each one backs up first, see
`/opt/mugkz/deploy-stage*.sh`). They change data, so a re-initialized database needs them again.

| Script | What |
|---|---|
| `01-exp-resets.sql` | Exp x300 (VIP x500), resets at 380 (VIP 360), 350 points per reset (Dark Lord 400), 5M zen |
| `02-spots-zen-standard.sql` | Zen per reset (~6M; global money rate 0.0046), the 3 strongest monsters of each farming map +50 %, Crywolf from level 280 |

`resetcalc.py` estimates the time and the zen of a reset with the spawns of the server
(export them with the query in its comment history, or ask for `spawns.csv`):
`python resetcalc.py spawns.csv`.
