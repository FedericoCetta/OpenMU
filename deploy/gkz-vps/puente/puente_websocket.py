"""MU GKZ - puente WebSocket a TCP para el cliente web.

El navegador no puede abrir conexiones TCP. El cliente web se conecta a este puente por WebSocket
(ws://host:puerto/puente?port=44405) y el puente abre la conexión TCP al servidor MU (OpenMU) y pasa los
bytes tal cual en los dos sentidos; el cifrado de los paquetes lo hacen el cliente y el servidor.

Seguridad: solo deja conectar a los puertos de la lista (connect server y game servers) del host del
servidor que se le indica; el navegador no elige a qué host se conecta. Sin dependencias: solo la
biblioteca estándar de Python.

    python puente_websocket.py --escuchar 127.0.0.1:8768 --servidor 127.0.0.1 --puertos 44405,45901-45906,45980
"""

import argparse
import asyncio
import base64
import hashlib
import logging
import struct
import urllib.parse

GUID = '258EAFA5-E914-47DA-95CA-C5AB0DC85B11'
OPCODE_CONTINUATION = 0x0
OPCODE_BINARY = 0x2
OPCODE_CLOSE = 0x8
OPCODE_PING = 0x9
OPCODE_PONG = 0xA
MAX_FRAME = 64 * 1024          # el paquete más grande del juego entra de sobra
HANDSHAKE_TIMEOUT = 10          # segundos para mandar el pedido de WebSocket
IDLE_TIMEOUT = 300              # segundos sin datos del navegador: el juego manda pings mucho antes
MAX_HEADER = 8 * 1024

log = logging.getLogger('puente')


def parse_ports(text):
    ports = set()
    for part in text.split(','):
        if '-' in part:
            first, last = part.split('-')
            ports.update(range(int(first), int(last) + 1))
        elif part:
            ports.add(int(part))
    return ports


async def read_handshake(reader):
    request = await asyncio.wait_for(reader.readuntil(b'\r\n\r\n'), HANDSHAKE_TIMEOUT)
    if len(request) > MAX_HEADER:
        raise ValueError('handshake too big')
    lines = request.decode('latin-1').split('\r\n')
    method, target, _ = lines[0].split(' ', 2)
    headers = {}
    for line in lines[1:]:
        if ':' in line:
            name, value = line.split(':', 1)
            headers[name.strip().lower()] = value.strip()
    return method, target, headers


def accept_key(key):
    return base64.b64encode(hashlib.sha1((key + GUID).encode()).digest()).decode()


def frame(opcode, payload):
    header = bytearray([0x80 | opcode])
    length = len(payload)
    if length < 126:
        header.append(length)
    elif length < 65536:
        header.append(126)
        header += struct.pack('>H', length)
    else:
        header.append(127)
        header += struct.pack('>Q', length)
    return bytes(header) + payload


async def read_frame(reader):
    first, second = await asyncio.wait_for(reader.readexactly(2), IDLE_TIMEOUT)
    opcode = first & 0x0F
    masked = second & 0x80
    length = second & 0x7F
    if length == 126:
        length = struct.unpack('>H', await reader.readexactly(2))[0]
    elif length == 127:
        length = struct.unpack('>Q', await reader.readexactly(8))[0]
    if length > MAX_FRAME:
        raise ValueError('frame too big')
    mask = await reader.readexactly(4) if masked else None
    payload = bytearray(await reader.readexactly(length))
    if mask:
        for i in range(length):
            payload[i] ^= mask[i % 4]
    return opcode, bytes(payload)


async def browser_to_server(ws_reader, ws_writer, tcp_writer):
    while True:
        opcode, payload = await read_frame(ws_reader)
        if opcode in (OPCODE_BINARY, OPCODE_CONTINUATION):
            tcp_writer.write(payload)
            await tcp_writer.drain()
        elif opcode == OPCODE_PING:
            ws_writer.write(frame(OPCODE_PONG, payload))
            await ws_writer.drain()
        elif opcode == OPCODE_CLOSE:
            return


async def server_to_browser(tcp_reader, ws_writer):
    while True:
        data = await tcp_reader.read(65536)
        if not data:
            return
        ws_writer.write(frame(OPCODE_BINARY, data))
        await ws_writer.drain()


class Limits:
    def __init__(self, maximum):
        self.maximum = maximum
        self.active = 0


async def handle(ws_reader, ws_writer, server, ports, origins, limits):
    peer = ws_writer.get_extra_info('peername')
    tcp_writer = None
    if limits.active >= limits.maximum:
        ws_writer.close()
        log.warning('rechazado %s: tope de %s conexiones', peer, limits.maximum)
        return
    limits.active += 1
    try:
        method, target, headers = await read_handshake(ws_reader)
        # Detrás de nginx, la IP real del jugador viene en X-Real-IP (solo para el registro).
        peer = headers.get('x-real-ip', peer)
        query = urllib.parse.parse_qs(urllib.parse.urlparse(target).query)
        port = int(query.get('port', ['0'])[0])
        origin = headers.get('origin', '')
        if method != 'GET' or 'sec-websocket-key' not in headers or port not in ports:
            ws_writer.write(b'HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\n\r\n')
            log.warning('rechazado %s: %s puerto %s', peer, method, port)
            return
        if origins and origin not in origins:
            ws_writer.write(b'HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\n\r\n')
            log.warning('rechazado %s: origen %s', peer, origin)
            return

        tcp_reader, tcp_writer = await asyncio.open_connection(server, port)
        ws_writer.write(('HTTP/1.1 101 Switching Protocols\r\n'
                         'Upgrade: websocket\r\nConnection: Upgrade\r\n'
                         f'Sec-WebSocket-Accept: {accept_key(headers["sec-websocket-key"])}\r\n\r\n').encode())
        await ws_writer.drain()
        log.info('conectado %s al puerto %s', peer, port)

        tasks = [asyncio.create_task(browser_to_server(ws_reader, ws_writer, tcp_writer)),
                 asyncio.create_task(server_to_browser(tcp_reader, ws_writer))]
        _, pending = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
        for task in pending:
            task.cancel()
    except (asyncio.IncompleteReadError, asyncio.LimitOverrunError, asyncio.TimeoutError, ConnectionError,
            ValueError) as error:
        log.info('fin %s: %s', peer, type(error).__name__)
    finally:
        limits.active -= 1
        if tcp_writer:
            tcp_writer.close()
        ws_writer.close()
        log.info('cerrado %s', peer)


async def main():
    parser = argparse.ArgumentParser(description='Puente WebSocket a TCP del cliente web de MU GKZ')
    parser.add_argument('--escuchar', default='127.0.0.1:8768', help='dirección:puerto donde escucha el puente')
    parser.add_argument('--servidor', default='127.0.0.1', help='host del servidor MU')
    parser.add_argument('--puertos', default='44405,45901-45906,45980', help='puertos permitidos del servidor MU')
    parser.add_argument('--origenes', default='', help='orígenes de la página permitidos (separados por coma)')
    parser.add_argument('--maximo', type=int, default=500, help='conexiones simultáneas como máximo')
    args = parser.parse_args()
    logging.basicConfig(level=logging.INFO, format='%(asctime)s %(message)s')

    host, port = args.escuchar.rsplit(':', 1)
    ports = parse_ports(args.puertos)
    origins = {o for o in args.origenes.split(',') if o}
    limits = Limits(args.maximo)
    server = await asyncio.start_server(lambda r, w: handle(r, w, args.servidor, ports, origins, limits), host,
                                        int(port), limit=MAX_HEADER)
    log.info('puente escuchando en %s, servidor %s, puertos %s', args.escuchar, args.servidor, sorted(ports))
    async with server:
        await server.serve_forever()


if __name__ == '__main__':
    asyncio.run(main())
