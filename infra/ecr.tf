# ECR — registro privado de imágenes Docker (ADR-09).
#
# Es el Docker Hub privado de AWS. GitHub Actions publica aquí la imagen que
# construye el Dockerfile del repositorio, y la EC2 la descarga usando su rol
# IAM: sin usuario ni contraseña de por medio.
#
# Se prefiere sobre Docker Hub por tres razones concretas:
#   · El repositorio es privado sin costo extra.
#   · La autenticación es por rol IAM, no por credenciales guardadas.
#   · La descarga viaja por la red interna de AWS: más rápida y sin cargo de
#     transferencia de datos.

resource "aws_ecr_repository" "app" {
  name = var.proyecto

  # MUTABLE permite reescribir la etiqueta "latest" en cada despliegue. Con
  # IMMUTABLE habría que inventar una etiqueta nueva siempre y el pipeline no
  # podría mantener un "latest" estable.
  image_tag_mutability = "MUTABLE"

  # Escaneo de vulnerabilidades en cada push. Analiza los paquetes del sistema
  # base de la imagen (Debian, en el caso de mcr.microsoft.com/dotnet/aspnet) y
  # reporta CVEs conocidos. El escaneo básico es gratuito.
  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name = "${var.proyecto}-ecr"
  }
}

# ===== Política de ciclo de vida: evita pagar por imágenes muertas =====
#
# ECR cobra $0.10 por GB-mes almacenado, y la imagen de GymTracker pesa 447 MB
# (medidos al construirla en local). Sin limpieza, cada despliegue sumaría otros
# 447 MB: a los 20 despliegues serían ~9 GB de imágenes que ya nadie usa, casi
# $1/mes creciendo sin freno.
#
# Conservar 5 versiones permite volver a una anterior si un despliegue sale mal,
# que es el único motivo real para guardar imágenes viejas.
resource "aws_ecr_lifecycle_policy" "app" {
  repository = aws_ecr_repository.app.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Conservar solo las ultimas 5 imagenes; borrar el resto"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = 5
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}
