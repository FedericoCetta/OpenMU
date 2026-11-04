# MU GKZ: el bot del Discord. Da la bienvenida a quien entra y atiende los tickets de soporte.
# Los ids salen de servidor.json (los genera armar_servidor.py); el token, de la variable DISCORD_TOKEN.
import io
import json
import logging
import os
import urllib.request
from datetime import datetime, timedelta, timezone

import discord

TOKEN = os.environ["DISCORD_TOKEN"]
IDS = {k: int(v) for k, v in json.load(open(os.path.join(os.path.dirname(__file__), "servidor.json"), encoding="utf-8")).items()}
COLOR = 0xB3121D
WEB = "https://mugkz.com"
IMAGEN = WEB + "/img/portada.webp"
URU_ARG = timezone(timedelta(hours=-3))
APERTURA = datetime(2026, 11, 4, 19, 0, tzinfo=URU_ARG)
TEMA = "ticket:{usuario}:{tipo}"

TIPOS = {
    "pagos": ("Pagos", "💳", discord.ButtonStyle.success,
              "Contanos qué pasó con tu pago: cuenta del juego, medio de pago, fecha y monto. Si tenés el comprobante, adjuntalo."),
    "bugs": ("Bugs", "🐞", discord.ButtonStyle.danger,
             "Contanos qué pasó, en qué mapa y cómo repetirlo. Si podés, sumá una captura o un video."),
    "cuenta": ("Cuenta", "👤", discord.ButtonStyle.primary,
               "Contanos qué problema tenés y cuál es el nombre de tu cuenta."),
}

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
log = logging.getLogger("gkz")


def contenido_activado():
    # Sin este permiso en el portal de Discord el bot ni conecta si lo pide, y los registros salen sin texto.
    pedido = urllib.request.Request("https://discord.com/api/v10/applications/@me", headers={
        "Authorization": "Bot " + TOKEN, "User-Agent": "DiscordBot (https://mugkz.com, 1.0)"})
    with urllib.request.urlopen(pedido) as respuesta:
        return bool(json.load(respuesta)["flags"] & ((1 << 18) | (1 << 19)))


def es_staff(miembro):
    return any(r.id in (IDS["rol_admin"], IDS["rol_soporte"]) for r in miembro.roles) or miembro.guild_permissions.administrator


def datos_ticket(canal):
    partes = (canal.topic or "").split(":")
    if len(partes) == 3 and partes[0] == "ticket" and partes[1].isdigit():
        return int(partes[1]), partes[2]
    return None


class PanelTickets(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None)
        for clave, (titulo, emoji, estilo, _) in TIPOS.items():
            boton = discord.ui.Button(label=titulo, emoji=emoji, style=estilo, custom_id="ticket:abrir:" + clave)
            boton.callback = self.abrir_con(clave)
            self.add_item(boton)

    def abrir_con(self, clave):
        async def abrir(interaccion):
            await abrir_ticket(interaccion, clave)
        return abrir


class TicketAbierto(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None)

    @discord.ui.button(label="Cerrar ticket", emoji="🔒", style=discord.ButtonStyle.secondary, custom_id="ticket:cerrar")
    async def cerrar(self, interaccion, _):
        datos = datos_ticket(interaccion.channel)
        if not datos or (interaccion.user.id != datos[0] and not es_staff(interaccion.user)):
            await interaccion.response.send_message("Solo quien abrió el ticket o el equipo de soporte pueden cerrarlo.", ephemeral=True)
            return
        await interaccion.response.send_message("¿Seguro que querés cerrar este ticket?", view=ConfirmarCierre(), ephemeral=True)


class ConfirmarCierre(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=60)

    @discord.ui.button(label="Sí, cerrar", style=discord.ButtonStyle.danger)
    async def si(self, interaccion, _):
        await interaccion.response.edit_message(content="Cerrando el ticket…", view=None)
        await cerrar_ticket(interaccion)


async def abrir_ticket(interaccion, clave):
    servidor = interaccion.guild
    usuario = interaccion.user
    await interaccion.response.defer(ephemeral=True)
    categoria = servidor.get_channel(IDS["tickets_abiertos"])
    for canal in categoria.text_channels:
        datos = datos_ticket(canal)
        if datos and datos[0] == usuario.id:
            await interaccion.followup.send(f"Ya tenés un ticket abierto: {canal.mention}", ephemeral=True)
            return

    titulo, emoji, _, pedido = TIPOS[clave]
    permisos = {
        servidor.default_role: discord.PermissionOverwrite(view_channel=False),
        servidor.me: discord.PermissionOverwrite(view_channel=True, send_messages=True, read_message_history=True),
        usuario: discord.PermissionOverwrite(view_channel=True, send_messages=True, read_message_history=True, attach_files=True, embed_links=True),
    }
    for rol_id in ("rol_admin", "rol_soporte"):
        permisos[servidor.get_role(IDS[rol_id])] = discord.PermissionOverwrite(view_channel=True, send_messages=True, read_message_history=True, attach_files=True)
    try:
        canal = await categoria.create_text_channel(f"{emoji}┃{clave}-{usuario.name}"[:90], overwrites=permisos, topic=TEMA.format(usuario=usuario.id, tipo=clave))
    except discord.HTTPException:
        log.exception("No se pudo crear el ticket")
        await interaccion.followup.send("No pudimos abrir el ticket. Probá de nuevo en unos minutos o escribinos a soporte@mugkz.com.", ephemeral=True)
        return

    mensaje = discord.Embed(title=f"{emoji} Ticket de {titulo}", description=f"{pedido}\n\n**Nunca compartas tu contraseña:** el equipo de MU GKZ no te la va a pedir.", color=COLOR)
    await canal.send(content=f"{usuario.mention} <@&{IDS['rol_soporte']}>", embed=mensaje, view=TicketAbierto(),
                     allowed_mentions=discord.AllowedMentions(users=[usuario], roles=[servidor.get_role(IDS["rol_soporte"])]))
    await interaccion.followup.send(f"Listo, abrimos tu ticket: {canal.mention}", ephemeral=True)


async def cerrar_ticket(interaccion):
    canal = interaccion.channel
    datos = datos_ticket(canal)
    if not datos:
        return
    lineas = []
    async for m in canal.history(limit=None, oldest_first=True):
        adjuntos = " ".join(a.url for a in m.attachments)
        lineas.append(f"[{m.created_at.astimezone(URU_ARG):%Y-%m-%d %H:%M}] {m.author} ({m.author.id}): {m.content} {adjuntos}".rstrip())

    titulo = TIPOS.get(datos[1], (datos[1],))[0]
    resumen = discord.Embed(title=f"Ticket cerrado: {titulo}", color=COLOR)
    resumen.add_field(name="Abierto por", value=f"<@{datos[0]}>")
    resumen.add_field(name="Cerrado por", value=interaccion.user.mention)
    resumen.add_field(name="Mensajes", value=str(len(lineas)))
    archivo = discord.File(io.BytesIO("\n".join(lineas).encode("utf-8")), filename=f"{canal.name}.txt")
    try:
        await interaccion.guild.get_channel(IDS["registros"]).send(embed=resumen, file=archivo, allowed_mentions=discord.AllowedMentions.none())
    except discord.HTTPException:
        # Sin el registro guardado el canal no se borra, para no perder la conversación.
        log.exception("No se pudo guardar el registro del ticket")
        await interaccion.followup.send("No se pudo guardar el registro, así que el ticket sigue abierto.", ephemeral=True)
        return
    await canal.delete(reason=f"Ticket cerrado por {interaccion.user}")


async def publicar_panel():
    canal = cliente.get_channel(IDS["tickets"])
    mensaje = discord.Embed(
        title="🎫 Soporte de MU GKZ",
        description="¿Necesitás ayuda? Elegí el tema y abrimos un canal privado entre vos y el equipo de soporte.\n\n"
                    "💳 **Pagos:** coins, VIP y compras\n🐞 **Bugs:** errores del juego o de la web\n👤 **Cuenta:** acceso y personajes\n\n"
                    "Nunca compartas tu contraseña: el equipo de MU GKZ no te la va a pedir.",
        color=COLOR)
    async for anterior in canal.history(limit=20):
        if anterior.author == cliente.user:
            await anterior.edit(embed=mensaje, view=PanelTickets())
            return
    await canal.send(embed=mensaje, view=PanelTickets())


class Cliente(discord.Client):
    async def setup_hook(self):
        self.add_view(PanelTickets())
        self.add_view(TicketAbierto())

    async def on_ready(self):
        log.info("Conectado como %s", self.user)
        await publicar_panel()

    async def on_member_join(self, miembro):
        if miembro.guild.id != IDS["servidor"] or miembro.bot:
            return
        if datetime.now(URU_ARG) < APERTURA:
            apertura = "**Abrimos el miércoles 4 de noviembre a las 19 h (URU-ARG).**\nCreá tu cuenta desde ahora y preparate para el primer día."
        else:
            apertura = "**Ya estamos abiertos.** Creá tu cuenta y entrá a jugar."
        mensaje = discord.Embed(
            title="¡Bienvenido a MU GKZ!",
            description=f"MU Season 3 old school, con premios por resets.\n\n{apertura}\n\n"
                        f"🌐 **Web y registro:** {WEB}\n📢 Enterate de todo en <#{IDS['anuncios']}>\n🎫 ¿Necesitás ayuda? Abrí un ticket en <#{IDS['tickets']}>",
            color=COLOR)
        mensaje.set_image(url=IMAGEN)
        await self.get_channel(IDS["bienvenidas"]).send(content=miembro.mention, embed=mensaje, allowed_mentions=discord.AllowedMentions(users=[miembro]))


intenciones = discord.Intents.none()
intenciones.guilds = True
intenciones.members = True
intenciones.guild_messages = True
intenciones.message_content = contenido_activado()
log.info("Contenido de mensajes: %s", intenciones.message_content)
cliente = Cliente(intents=intenciones, allowed_mentions=discord.AllowedMentions.none())
cliente.run(TOKEN, log_handler=None)
