# Configuración base de Terraform (ADR-09).
#
# Este archivo no crea NINGÚN recurso: fija las versiones de las herramientas y
# le dice a Terraform dónde guardar su estado.

terraform {
  # Versión mínima de Terraform. Se fija por la misma razón que el workflow de CI
  # fija dotnet-version '10.0.x': que la infraestructura no cambie de
  # comportamiento porque una herramienta se actualizó sola.
  required_version = ">= 1.9.0"

  required_providers {
    aws = {
      source = "hashicorp/aws"
      # El operador ~> permite parches y versiones menores (6.1, 6.2...) pero
      # NUNCA un salto mayor a 7.x, que podría traer cambios incompatibles.
      version = "~> 6.0"
    }
  }

  # ===== Estado remoto en S3 =====
  # El "estado" es el mapa de Terraform entre lo que está escrito en estos
  # archivos y lo que realmente existe en AWS (IDs de instancia, ARNs, IPs).
  #
  # Vive en S3 y no en el disco local por una razón concreta: si el archivo de
  # estado se pierde, Terraform OLVIDA que los recursos existen y ya no puede
  # destruirlos. Seguirían encendidos y cobrando, y habría que borrarlos a mano
  # uno por uno desde la consola. El bucket tiene versionado activado, así que
  # incluso un estado corrupto se puede recuperar.
  #
  # El bucket se creó A MANO, una sola vez, porque Terraform no puede crear el
  # sitio donde guarda su propio estado (problema del huevo y la gallina). Por lo
  # mismo, NO se borra con `terraform destroy`: es lo que hace seguro destruir
  # todo lo demás.
  backend "s3" {
    bucket = "gymtracker-tfstate-602440904865"
    key    = "gymtracker/terraform.tfstate"
    region = "us-east-1"

    # Bloqueo por archivo (S3 nativo, desde Terraform 1.10). Impide que dos
    # `apply` simultáneos corrompan el estado. Antes esto exigía una tabla de
    # DynamoDB aparte; ya no, así que nos ahorramos ese recurso.
    use_lockfile = true

    # El estado contiene la contraseña de RDS EN TEXTO PLANO — es una limitación
    # conocida de Terraform, no un descuido. Por eso el bucket está cifrado con
    # AES-256 y tiene bloqueado todo acceso público.
    encrypt = true
  }
}

provider "aws" {
  region = var.region

  # Etiquetas aplicadas AUTOMÁTICAMENTE a todos los recursos que soporten
  # etiquetado. Sirven para dos cosas concretas:
  #   1. Filtrar el gasto por proyecto en la consola de Billing.
  #   2. Reconocer de un vistazo qué recursos son de GymTracker y cuáles no,
  #      algo que importa cuando se busca un recurso olvidado que está cobrando.
  default_tags {
    tags = {
      Proyecto  = "GymTracker"
      Gestion   = "Terraform"
      Ambiente  = var.ambiente
    }
  }
}
