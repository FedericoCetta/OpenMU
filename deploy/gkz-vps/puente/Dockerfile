# MU GKZ: el puente WebSocket a TCP del cliente web (jugar.mugkz.com). Sin dependencias: solo Python.
# Corre sin privilegios; nginx es el único que le habla (red interna de Docker, sin puertos publicados).
FROM python:3.12.7-alpine3.20

RUN adduser -D -H -u 10001 puente
COPY puente_websocket.py /app/puente_websocket.py

USER puente
# -I: Python aislado (no lee variables de entorno ni la carpeta actual).
ENTRYPOINT ["python", "-I", "/app/puente_websocket.py"]
