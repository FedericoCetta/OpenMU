#!/bin/sh
# MU GKZ: publica la web estática sin reiniciar nada.
#
# Uso (desde la PC, en OpenMU/deploy/gkz-vps):
#   tar -cf - web | ssh root@mugkz.com 'sh /opt/mugkz/deploy-web.sh'
#
# Importante: la carpeta /opt/mugkz/web está montada en el contenedor de nginx. Si se borra y se
# vuelve a crear, nginx sigue viendo la carpeta vieja (vacía) y la web da 403. Por eso este script
# reemplaza el contenido de la carpeta, nunca la carpeta.
set -e

STAGE=/opt/stage/web-deploy
rm -rf "$STAGE"
mkdir -p "$STAGE"
tar -xf - -C "$STAGE"

# Los archivos vienen de Windows: sin finales de línea CRLF.
find "$STAGE" -type f \( -name "*.html" -o -name "*.css" -o -name "*.js" \) -exec sed -i 's/\r$//' {} +

# Copia de lo que estaba, por las dudas.
BACKUP=/opt/mugkz/backups/web-$(date +%Y%m%d-%H%M%S)
cp -a /opt/mugkz/web "$BACKUP"

# Reemplazar el contenido sin tocar la carpeta montada.
find /opt/mugkz/web -mindepth 1 -delete
cp -a "$STAGE"/web/. /opt/mugkz/web/
chmod -R a+rX /opt/mugkz/web

if docker exec nginx test -f /usr/share/nginx/gkz-web/index.html; then
  echo "Web publicada (copia anterior en $BACKUP)."
else
  echo "nginx no ve la web: reiniciándolo." >&2
  docker restart nginx >/dev/null
fi
