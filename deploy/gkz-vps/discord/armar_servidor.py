# MU GKZ: arma las categorías, canales, permisos y seguridad del Discord (pedido por Federico, 2026-11-03).
# Se puede correr varias veces: lo que ya existe se actualiza, y no borra ningún canal.
# El token se lee del archivo de claves y nunca se imprime. Guarda los ids en servidor.json (no son secretos).
import json
import os
import time
import urllib.error
import urllib.request

ARCHIVO_TOKEN = os.path.expandvars(r"%USERPROFILE%\.mu-gkz\keys\discord-bot.txt")
TOKEN = open(ARCHIVO_TOKEN, encoding="utf-8-sig").read().strip()
SERVIDOR = "1564466884450189485"
ROL_ADMIN = "1564467683997651085"
ROL_BOT = "1567154153233453120"

VER = 1 << 10
ESCRIBIR = 1 << 11
REACCIONAR = 1 << 6
HISTORIAL = 1 << 16
ENLACES = 1 << 14
ADJUNTOS = 1 << 15
GESTIONAR_CANALES = 1 << 4
GESTIONAR_ROLES = 1 << 28
MENCIONAR_TODOS = 1 << 17
HILOS = (1 << 35) | (1 << 36) | (1 << 38)
SOLO_LECTURA = ESCRIBIR | HILOS


def api(metodo, ruta, datos=None):
    cuerpo = None if datos is None else json.dumps(datos).encode("utf-8")
    pedido = urllib.request.Request(
        "https://discord.com/api/v10" + ruta, data=cuerpo, method=metodo,
        headers={"Authorization": "Bot " + TOKEN, "User-Agent": "DiscordBot (https://mugkz.com, 1.0)", "Content-Type": "application/json"})
    while True:
        try:
            with urllib.request.urlopen(pedido) as respuesta:
                texto = respuesta.read()
                return json.loads(texto) if texto else None
        except urllib.error.HTTPError as error:
            if error.code != 429:
                raise SystemExit(f"Discord rechazó {metodo} {ruta}: {error.code} {error.read()[:300]}")
            time.sleep(float(json.loads(error.read()).get("retry_after", 1)) + 0.2)


def nombre(emoji, texto):
    # Discord no acepta la barra común ni espacios en el nombre de un canal.
    return f"{emoji}┃{texto}"


def permisos(*lista):
    return [{"id": i, "type": 0, "allow": str(a), "deny": str(d)} for i, a, d in lista]


canales = api("GET", f"/guilds/{SERVIDOR}/channels")
roles = api("GET", f"/guilds/{SERVIDOR}/roles")


def buscar(palabra, tipo):
    return next((c for c in canales if palabra in c["name"].lower() and c["type"] == tipo), None)


def asegurar(palabra, tipo, datos):
    actual = buscar(palabra, tipo)
    if actual:
        canal = api("PATCH", f"/channels/{actual['id']}", datos)
        print("actualizado:", ascii(canal["name"]))
    else:
        canal = api("POST", f"/guilds/{SERVIDOR}/channels", {**datos, "type": tipo})
        canales.append(canal)
        print("creado:", ascii(canal["name"]))
    return canal


# Rol del equipo de soporte: junto con Admin es el que ve los tickets.
soporte = next((r for r in roles if r["name"] == "Soporte"), None)
if not soporte:
    soporte = api("POST", f"/guilds/{SERVIDOR}/roles", {"name": "Soporte", "color": 0xC9A227, "hoist": True, "mentionable": True, "permissions": "0"})
    print("rol creado: Soporte")
ROL_SOPORTE = soporte["id"]

# Los jugadores no pueden mencionar a todo el servidor; el resto de sus permisos no se toca.
todos = next(r for r in roles if r["id"] == SERVIDOR)
api("PATCH", f"/guilds/{SERVIDOR}/roles/{SERVIDOR}", {"permissions": str(int(todos["permissions"]) & ~MENCIONAR_TODOS)})

# Verificación media, filtro de contenido para todos, y sin el aviso de entrada de Discord (lo da el bot).
api("PATCH", f"/guilds/{SERVIDOR}", {"verification_level": 2, "explicit_content_filter": 2, "system_channel_flags": 1})

info = asegurar("informaci", 4, {"name": "💡 INFORMACIÓN", "position": 0})
servidor = asegurar("servidor", 4, {"name": "⚙️ SERVIDOR", "position": 1})
soporte_cat = asegurar("soporte", 4, {"name": "🛠️ SOPORTE", "position": 2})

solo_lectura = permisos((SERVIDOR, 0, SOLO_LECTURA))
# El bot necesita permisos propios donde @everyone no escribe o no ve: sin Administrador, esa prohibición también lo alcanza.
DEL_BOT = (ROL_BOT, VER | ESCRIBIR | HISTORIAL | ENLACES | ADJUNTOS | GESTIONAR_CANALES | GESTIONAR_ROLES, 0)
bienvenidas = asegurar("bienvenid", 0, {"name": nombre("💥", "bienvenidas"), "parent_id": info["id"], "position": 0, "topic": "Bienvenidos a MU GKZ, Season 3 old school.", "permission_overwrites": permisos((SERVIDOR, 0, SOLO_LECTURA), DEL_BOT)})
anuncios = asegurar("anuncios", 0, {"name": nombre("📢", "anuncios"), "parent_id": servidor["id"], "position": 0, "topic": "Las novedades oficiales de MU GKZ.", "permission_overwrites": solo_lectura})
sorteos = asegurar("sorteos", 0, {"name": nombre("🎁", "sorteos"), "parent_id": servidor["id"], "position": 1, "topic": "Sorteos y premios para la comunidad.", "permission_overwrites": solo_lectura})
changelogs = asegurar("changelogs", 0, {"name": nombre("⚙️", "changelogs"), "parent_id": servidor["id"], "position": 2, "topic": "Los cambios de cada actualización.", "permission_overwrites": solo_lectura})
eventos = asegurar("eventos", 0, {"name": nombre("🎉", "eventos"), "parent_id": servidor["id"], "position": 3, "topic": "Eventos del servidor y de la comunidad.", "permission_overwrites": solo_lectura})
sugerencias = asegurar("sugerencias", 0, {"name": nombre("📄", "sugerencias"), "parent_id": servidor["id"], "position": 4, "topic": "Contanos qué le sumarías a MU GKZ."})
tickets = asegurar("tickets", 0, {"name": nombre("🎫", "tickets"), "parent_id": soporte_cat["id"], "position": 0, "topic": "Abrí un ticket y el equipo de soporte te responde en privado.", "permission_overwrites": permisos((SERVIDOR, 0, SOLO_LECTURA | REACCIONAR), DEL_BOT)})

# Lo privado queda al final: solo lo ven Admin y Soporte; cada ticket suma además a quien lo abrió.
privado = permisos((SERVIDOR, 0, VER), (ROL_ADMIN, VER | ESCRIBIR, 0), (ROL_SOPORTE, VER | ESCRIBIR, 0), DEL_BOT)
abiertos = asegurar("tickets abiertos", 4, {"name": "🎫 TICKETS ABIERTOS", "permission_overwrites": privado})
staff = asegurar("staff", 4, {"name": "🔒 STAFF", "permission_overwrites": privado})
registros = asegurar("registros", 0, {"name": nombre("📁", "registros"), "parent_id": staff["id"], "topic": "Historial de los tickets cerrados.", "permission_overwrites": privado})

# Las categorías que ya existían quedan debajo de las nuevas.
orden = [info, servidor, soporte_cat] + [c for c in canales if c["type"] == 4 and c["id"] in ("1564466887247667230", "1564466887247667233")] + [abiertos, staff]
api("PATCH", f"/guilds/{SERVIDOR}/channels", [{"id": c["id"], "position": i} for i, c in enumerate(orden)])

ids = {"servidor": SERVIDOR, "rol_admin": ROL_ADMIN, "rol_soporte": ROL_SOPORTE, "bienvenidas": bienvenidas["id"], "anuncios": anuncios["id"],
       "tickets": tickets["id"], "tickets_abiertos": abiertos["id"], "registros": registros["id"]}
with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "servidor.json"), "w", encoding="utf-8") as archivo:
    json.dump(ids, archivo, indent=2)
print("Listo.")
