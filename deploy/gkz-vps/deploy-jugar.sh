#!/bin/sh
# MU GKZ: publica una versión del juego en el navegador (jugar.mugkz.com) sin reiniciar nada.
#
# Uso (desde la PC, en la carpeta con Main.html, Main.js, Main.wasm, Main.data, gkz-web.js y gkz-web.css):
#   tar -cf - Main.html Main.js Main.wasm Main.data gkz-web.js gkz-web.css | ssh root@mugkz.com 'sh /opt/mugkz/deploy-jugar.sh'
#
# Como deploy-web.sh: la carpeta /opt/mugkz/jugar está montada en nginx, así que se reemplaza su
# contenido, nunca la carpeta. Cada archivo va también comprimido (.gz) para que baje más rápido.
set -e

STAGE=/opt/stage/jugar-deploy
rm -rf "$STAGE"
mkdir -p "$STAGE"
tar -xf - -C "$STAGE"

for file in Main.html Main.js Main.wasm Main.data gkz-web.js gkz-web.css; do
  if [ ! -f "$STAGE/$file" ]; then
    echo "Falta $file: no se publica nada." >&2
    exit 1
  fi
done

find "$STAGE" -type f \( -name "*.html" -o -name "*.css" -o -name "*.js" \) -exec sed -i 's/\r$//' {} +
for file in "$STAGE"/*; do
  gzip -9 -k "$file"
done

find /opt/mugkz/jugar -mindepth 1 -delete
cp -a "$STAGE"/. /opt/mugkz/jugar/
chmod -R a+rX /opt/mugkz/jugar
echo "Juego publicado en jugar.mugkz.com."
