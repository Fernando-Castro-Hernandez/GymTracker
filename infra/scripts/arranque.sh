# Instalación y preparación de la EC2 (ADR-09).
#
# Lo ejecuta cloud-init UNA SOLA VEZ, cuando la instancia nace. Convierte un
# Amazon Linux recién creado en un servidor listo para recibir despliegues.
#
# Esto es lo que hace REPRODUCIBLE la infraestructura: `terraform destroy`
# seguido de `apply` devuelve un servidor idéntico en minutos, sin configurar
# nada a mano ni recordar qué se instaló.
#
# NOTA: este archivo NO lleva shebang ni `set -euo pipefail`. No se ejecuta
# suelto: se inserta dentro de scripts/user-data.sh.tftpl, que ya los declara al
# principio. Un segundo shebang a media plantilla sería sólo un comentario.
#
# La salida queda en /var/log/cloud-init-output.log, útil para diagnosticar.

echo "===== Arranque de GymTracker: $(date) ====="

# ===== Actualizar el sistema =====
dnf update -y

# ===== Docker =====
# La app y Caddy corren en contenedores, igual que en desarrollo.
dnf install -y docker
systemctl enable --now docker

# El usuario ec2-user puede usar docker sin sudo (comodidad al diagnosticar
# por SSM Session Manager).
usermod -aG docker ec2-user

# ===== Docker Compose v2 =====
# Amazon Linux 2023 no lo trae en sus repositorios: se instala como plugin de la
# CLI de Docker, que es la forma soportada actualmente (`docker compose`, sin
# guion, en vez del viejo `docker-compose`).
mkdir -p /usr/local/lib/docker/cli-plugins
curl -SL "https://github.com/docker/compose/releases/download/v2.32.4/docker-compose-linux-x86_64" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

# ===== Agente SSM =====
# Amazon Linux 2023 ya lo trae preinstalado; se asegura de que esté activo.
# Es lo que permite entrar a la instancia SIN SSH y sin puerto 22 abierto: el
# agente abre una conexión SALIENTE hacia AWS y la terminal viaja por ese túnel.
systemctl enable --now amazon-ssm-agent

# ===== Directorio de la aplicación =====
mkdir -p /opt/gymtracker
cd /opt/gymtracker

# ===== Script que lee los secretos de SSM Parameter Store =====
# Aquí se cierra el círculo del manejo de secretos: el docker-compose.prod.yml
# espera variables como ConnectionStrings__DefaultConnection, y este script las
# obtiene de Parameter Store usando el ROL DE INSTANCIA — sin ninguna credencial
# de AWS escrita en el servidor.
#
# El archivo .env resultante NUNCA sale de la máquina y se regenera en cada
# despliegue, así que rotar un secreto es cambiarlo en SSM y redesplegar.
cat > /opt/gymtracker/cargar-secretos.sh <<'SCRIPT'
#!/bin/bash
set -euo pipefail

REGION="${AWS_REGION:-us-east-1}"
PREFIJO="/gymtracker"
DESTINO="/opt/gymtracker/.env"

echo "Leyendo secretos de SSM Parameter Store..."

# --with-decryption descifra los parámetros SecureString. Funciona gracias al
# permiso kms:Decrypt del rol de instancia, acotado por condición a que la
# petición venga de SSM.
leer() {
  aws ssm get-parameter \
    --name "${PREFIJO}/$1" \
    --with-decryption \
    --region "$REGION" \
    --query "Parameter.Value" \
    --output text
}

# Se escribe primero a un archivo temporal: si algún parámetro falta, el .env
# anterior queda intacto en vez de quedar a medias.
TEMPORAL=$(mktemp)
{
  echo "IMAGEN_URI=$(leer imagen-uri)"
  echo "DOMINIO=$(leer dominio)"
  echo "ConnectionStrings__DefaultConnection=$(leer connection-string)"
  echo "Anthropic__ApiKey=$(leer anthropic-api-key)"
  echo "Gemini__ApiKey=$(leer gemini-api-key)"
} > "$TEMPORAL"

mv "$TEMPORAL" "$DESTINO"
# Sólo root puede leerlo: contiene la contraseña de la base y las API keys.
chmod 600 "$DESTINO"

echo "Secretos cargados en $DESTINO"
SCRIPT

chmod +x /opt/gymtracker/cargar-secretos.sh

# ===== Script de despliegue =====
# Lo invoca GitHub Actions por `aws ssm send-command`. Reemplaza al
# `ssh servidor "docker compose pull && up -d"` de un despliegue tradicional.
cat > /opt/gymtracker/desplegar.sh <<'SCRIPT'
#!/bin/bash
set -euo pipefail

cd /opt/gymtracker

echo "===== Despliegue: $(date) ====="

# 1. Refrescar los secretos (la etiqueta de la imagen cambia en cada despliegue)
./cargar-secretos.sh

# 2. Autenticarse contra ECR con el rol de instancia. El token dura 12 horas y
#    lo obtiene la propia máquina: no hay contraseña de registro guardada.
source /opt/gymtracker/.env
REGISTRO=$(echo "$IMAGEN_URI" | cut -d'/' -f1)
aws ecr get-login-password --region "${AWS_REGION:-us-east-1}" \
  | docker login --username AWS --password-stdin "$REGISTRO"

# 3. Descargar la imagen nueva y reemplazar los contenedores
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d

# 4. Borrar las imágenes viejas, que si no llenan el disco de 30 GB
docker image prune -af

echo "===== Despliegue terminado: $(date) ====="
docker compose -f docker-compose.prod.yml ps
SCRIPT

chmod +x /opt/gymtracker/desplegar.sh

# ===== NO se arranca la aplicación aquí =====
# El user_data corre ANTES de que existan los secretos en Parameter Store y
# antes de que el pipeline haya publicado una imagen en ECR. Intentar levantar
# la app en este punto fallaría siempre.
#
# La máquina queda preparada; el primer despliegue la pone en marcha.

echo "===== Instancia lista para recibir despliegues: $(date) ====="
