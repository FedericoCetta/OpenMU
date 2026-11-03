# MU GKZ: el bot del Discord. Corre sin privilegios y solo habla con Discord (sin puertos publicados).
FROM python:3.12.7-alpine3.20

RUN pip install --no-cache-dir discord.py==2.5.2 && adduser -D -H -u 10002 gkzbot
COPY bot.py servidor.json /app/

USER gkzbot
# -u: los registros salen al momento.
ENTRYPOINT ["python", "-u", "/app/bot.py"]
