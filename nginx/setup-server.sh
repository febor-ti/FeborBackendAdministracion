#!/usr/bin/env bash
# setup-server.sh — ejecutado por GitHub Actions en cada deploy.
# Crea directorios y actualiza appsettings.Production.json con las secciones de cursos.

set -e

FEBOR_DIR="/var/www/febor"
COURSES_DIR="/var/www/febor/cursos"
APPSETTINGS="/opt/feborbackadmin/appsettings.Production.json"
NGINX_SITE="/etc/nginx/sites-enabled/febor.conf"

# ── 1. Directorios ────────────────────────────────────────────────────────────
echo "[1/3] Creando directorios..."
sudo mkdir -p "$COURSES_DIR"
sudo chown -R deploy:www-data "$FEBOR_DIR"
sudo chmod -R 775 "$FEBOR_DIR"
echo "      OK: $COURSES_DIR listo"

# ── 2. appsettings.Production.json ───────────────────────────────────────────
echo "[2/3] Actualizando appsettings.Production.json..."
if [ -f "$APPSETTINGS" ]; then
    python3 - <<'PYEOF'
import json

path = '/opt/feborbackadmin/appsettings.Production.json'
with open(path, 'r') as f:
    cfg = json.load(f)

cfg['Courses'] = {
    'BasePath': 'C:\\Febor\\Cursos',
    'ProductionBasePath': '/var/www/febor/cursos',
    'BaseUrl': 'https://virtual.febor.co/cursos'
}
cfg['ErrorPages'] = {
    'NotFoundPath': 'C:\\Febor\\404.html',
    'ProductionNotFoundPath': '/var/www/febor/404.html'
}

with open(path, 'w') as f:
    json.dump(cfg, f, indent=4, ensure_ascii=False)

print('      OK: Courses y ErrorPages inyectados')
PYEOF
else
    echo "      WARN: $APPSETTINGS no encontrado, omitiendo"
fi

# ── 3. Nginx ──────────────────────────────────────────────────────────────────
echo "[3/3] Verificando Nginx..."
sudo nginx -t && sudo systemctl reload nginx
echo "      OK: Nginx recargado"

echo ""
echo "Setup completado."
