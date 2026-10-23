#!/bin/sh
# MU GKZ: prepara jugar.mugkz.com (el juego en el navegador, prueba privada). Se corre una vez en el VPS,
# en /opt/mugkz, después de copiar los archivos nuevos del repo (nginx/gkz-jugar*.conf, puente/,
# docker-compose.yml). No reinicia el servidor del juego: solo crea el puente y recrea nginx (segundos).
#
#   sh setup-jugar.sh            -> usuario de la prueba, puente, nginx
#   sh setup-jugar.sh certificado -> pide el certificado de jugar.mugkz.com y activa el sitio
#                                    (cuando el DNS de jugar.mugkz.com ya apunta al VPS)
set -e
cd /opt/mugkz

if [ "$1" = "certificado" ]; then
  docker run --rm \
    -v /opt/mugkz/certbot/conf:/etc/letsencrypt \
    -v /opt/mugkz/certbot/www:/var/www/certbot \
    certbot/certbot certonly --webroot -w /var/www/certbot \
    -d jugar.mugkz.com --non-interactive --agree-tos --register-unsafely-without-email --keep-until-expiring
  cp /opt/stage/gkz-jugar-servers.conf nginx/gkz-jugar-servers.conf
  docker exec nginx nginx -t
  docker exec nginx nginx -s reload
  echo "jugar.mugkz.com activo."
  exit 0
fi

mkdir -p jugar secrets
# Mientras no haya certificado, el sitio no existe (el archivo de sus servidores queda vacío).
if [ ! -f nginx/gkz-jugar-servers.conf ] || [ ! -f certbot/conf/live/jugar.mugkz.com/fullchain.pem ]; then
  mkdir -p /opt/stage
  mv nginx/gkz-jugar-servers.conf /opt/stage/gkz-jugar-servers.conf 2>/dev/null || true
  : > nginx/gkz-jugar-servers.conf
fi

# El usuario de la prueba privada: contraseña al azar, guardada solo acá (no en git).
if [ ! -f nginx/jugar.htpasswd ]; then
  PASSWORD=$(openssl rand -base64 18 | tr -d '/+=' | cut -c1-20)
  printf 'federico:%s\n' "$(openssl passwd -apr1 "$PASSWORD")" > nginx/jugar.htpasswd
  printf 'https://jugar.mugkz.com\nusuario: federico\ncontraseña: %s\n' "$PASSWORD" > secrets/jugar-prueba.txt
  chmod 600 secrets/jugar-prueba.txt
  chmod 644 nginx/jugar.htpasswd
  echo "Usuario de la prueba creado (ver secrets/jugar-prueba.txt)."
fi

if [ ! -f jugar/Main.html ]; then
  printf '<!doctype html><title>MU GKZ</title><p>Pronto.</p>\n' > jugar/Main.html
fi
chmod -R a+rX jugar

docker compose build puente
docker compose up -d puente
docker compose up -d nginx
docker exec nginx nginx -t
echo "Puente y nginx listos."
