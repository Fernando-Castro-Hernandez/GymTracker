# EC2 — el servidor que corre la aplicación (ADR-09).
#
# Una sola instancia con Docker, que ejecuta dos contenedores: Caddy (proxy
# inverso y TLS) y GymTracker. Es el modelo más simple que cumple el objetivo, y
# el que se descartó reemplazar por ECS Fargate + ALB por costo (~$45-60/mes sin
# resolver ningún problema real de una app de un usuario).

# ===== La imagen del sistema operativo =====
# Se consulta el AMI más reciente en vez de fijar un ID, porque los IDs cambian
# con cada actualización de Amazon Linux y son distintos en cada región.
#
# Amazon Linux 2023: mantenido por AWS, trae el agente SSM preinstalado y
# recibe parches de seguridad sin costo.
data "aws_ami" "amazon_linux" {
  most_recent = true
  owners      = ["amazon"]

  # El patrón es "al2023-ami-2023.*" y no "al2023-ami-*" porque el comodín amplio
  # también captura variantes como al2023-ami-ECS-*, optimizada para el servicio
  # ECS y con su agente preinstalado. Aquí se corre Docker directamente, así que
  # ese software sobra: menos componentes, menos superficie de ataque.
  filter {
    name   = "name"
    values = ["al2023-ami-2023.*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# ===== La instancia =====
resource "aws_instance" "app" {
  ami           = data.aws_ami.amazon_linux.id
  instance_type = var.tipo_instancia

  # ARQUITECTURA x86, no ARM. Aquí NO se repite el ahorro de Graviton que sí se
  # aplicó a RDS: la imagen Docker de la app se construye para linux/amd64 (ver
  # Dockerfile), y una instancia ARM no podría ejecutarla sin recompilar toda la
  # aplicación. El ahorro no compensa el riesgo.

  subnet_id              = aws_subnet.publica.id
  vpc_security_group_ids = [aws_security_group.ec2.id]

  # El rol IAM: le da acceso a ECR y a Parameter Store SIN ninguna credencial
  # escrita en el disco del servidor.
  iam_instance_profile = aws_iam_instance_profile.ec2.name

  # ===== Disco =====
  root_block_device {
    volume_size = var.tamano_disco_gb
    volume_type = "gp3" # Más barato y más rápido que gp2, sin contrapartida
    encrypted   = true  # Cifrado en reposo, sin costo adicional

    # Al destruir la instancia se borra el disco. Los datos que importan viven en
    # RDS, no aquí: en la EC2 sólo hay imágenes de Docker y logs.
    delete_on_termination = true
  }

  # ===== Script de primer arranque =====
  # cloud-init lo ejecuta una sola vez, al nacer la instancia. Instala Docker, el
  # agente SSM y deja los scripts de despliegue.
  #
  # Los archivos de deploy/ se insertan para mantener UNA SOLA fuente de verdad:
  # se editan en el repositorio y llegan al servidor sin copiarlos a mano (algo
  # que además requeriría SSH, que está cerrado).
  #
  # Se usa templatefile() y NO un heredoc aquí mismo porque el shebang
  # #!/bin/bash debe quedar en la columna 1. Con <<-EOT, Terraform elimina sólo
  # la sangría COMÚN a todas las líneas, y como los archivos insertados con
  # file() empiezan en columna 0, esa sangría común es cero: el shebang llegaba
  # con cuatro espacios delante y cloud-init fallaba en 9 segundos sin instalar
  # nada, sin que el `apply` reportara ningún error.
  user_data = templatefile("${path.module}/scripts/user-data.sh.tftpl", {
    caddyfile = file("${path.module}/../deploy/Caddyfile")
    compose   = file("${path.module}/../deploy/docker-compose.prod.yml")
    arranque  = file("${path.module}/scripts/arranque.sh")
  })

  # Si cambia el user_data, Terraform RECREA la instancia. Es deliberado: un
  # script de arranque modificado no se vuelve a ejecutar en una máquina viva, y
  # una instancia recreada garantiza que el servidor coincide con el código.
  user_data_replace_on_change = true

  # ===== Metadatos: IMDSv2 obligatorio =====
  # El servicio de metadatos (169.254.169.254) es de donde la instancia obtiene
  # las credenciales temporales de su rol IAM.
  #
  # Exigir IMDSv2 (require_tokens) protege contra SSRF: con IMDSv1 bastaba con
  # que la app hiciera una petición a esa IP —engañada por una URL maliciosa—
  # para filtrar las credenciales del rol. IMDSv2 exige un token PUT previo, que
  # un ataque SSRF no puede emitir.
  metadata_options {
    http_endpoint = "enabled"
    http_tokens   = "required"
  }

  # Protección contra apagado accidental desde la consola. No impide el
  # `terraform destroy`, que es la vía prevista para apagar todo.
  disable_api_termination = false

  tags = {
    Name = "${var.proyecto}-app"
  }

  # La instancia depende de RDS: no tiene sentido tener servidor sin base. Lo
  # infiere Terraform por el grafo, pero se declara para dejarlo explícito.
  depends_on = [aws_db_instance.principal]
}

# ===== Elastic IP — la única dirección fija =====
#
# Cuando una EC2 se detiene y arranca, SU IP PÚBLICA CAMBIA. Una Elastic IP es
# una dirección reservada que permanece aunque se reemplace la instancia.
#
# Es lo que permite apuntar el dominio desde Cloudflare con un registro A y que
# siga funcionando después de un redespliegue.
#
# ⚠️ COSTO: ~$3.65/mes. Desde 2024 AWS cobra por toda IPv4 pública, esté en uso o
# no. Es el precio de tener una dirección estable.
resource "aws_eip" "app" {
  domain   = "vpc"
  instance = aws_instance.app.id

  tags = {
    Name = "${var.proyecto}-eip"
  }

  # El internet gateway debe existir antes de asociar una IP elástica.
  depends_on = [aws_internet_gateway.principal]
}
