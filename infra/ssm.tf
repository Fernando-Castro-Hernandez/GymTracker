# SSM Parameter Store — los secretos de producción (ADR-09).
#
# Es el reemplazo en producción de User Secrets, tal como establece la §7.3 de
# docs/PLAN-integraciones-IA.md. Los parámetros SecureString se cifran con KMS y
# la EC2 los lee con su rol de instancia, sin ninguna credencial en disco.
#
# ⚠️ CLAVE: Terraform crea los parámetros VACÍOS y `ignore_changes` le impide
# volver a tocar su valor. Los valores reales se cargan por CLI, de modo que
# NINGÚN secreto pasa por Terraform ni queda escrito en el estado.
#
# Es la misma regla que ya sigue el proyecto: los secretos nunca se versionan.
#
# Los parámetros estándar (hasta 4 KB) son GRATIS.

locals {
  # Marcador para crear el parámetro sin valor real. Se sobrescribe por CLI.
  pendiente = "PENDIENTE-cargar-por-cli"
}

# ===== Connection string de RDS =====
# Formato de Npgsql, el mismo que en User Secrets durante el desarrollo.
# Se carga con:
#   terraform output -raw connection_string
resource "aws_ssm_parameter" "connection_string" {
  name        = "/${var.proyecto}/connection-string"
  description = "Connection string de PostgreSQL en RDS"
  type        = "SecureString"
  value       = local.pendiente

  lifecycle {
    ignore_changes = [value]
  }

  tags = {
    Name = "${var.proyecto}-connection-string"
  }
}

# ===== API key de Anthropic (Claude) =====
# Proveedor principal del Coach IA y del chatbot (ADR-07).
resource "aws_ssm_parameter" "anthropic_api_key" {
  name        = "/${var.proyecto}/anthropic-api-key"
  description = "API key de Anthropic para el Coach IA y el chatbot"
  type        = "SecureString"
  value       = local.pendiente

  lifecycle {
    ignore_changes = [value]
  }

  tags = {
    Name = "${var.proyecto}-anthropic-api-key"
  }
}

# ===== API key de Gemini =====
# Proveedor de respaldo: ProveedorIAConFallback cae aquí si Claude falla.
resource "aws_ssm_parameter" "gemini_api_key" {
  name        = "/${var.proyecto}/gemini-api-key"
  description = "API key de Gemini, proveedor de respaldo de IA"
  type        = "SecureString"
  value       = local.pendiente

  lifecycle {
    ignore_changes = [value]
  }

  tags = {
    Name = "${var.proyecto}-gemini-api-key"
  }
}

# ===== URI de la imagen a desplegar =====
# NO es un secreto, pero vive aquí por comodidad: el pipeline de CD lo actualiza
# con la etiqueta del commit recién publicado, y el script de la EC2 lo lee junto
# con el resto. Cambiar este valor y redesplegar es lo que permite volver a una
# versión anterior si algo sale mal.
resource "aws_ssm_parameter" "imagen_uri" {
  name        = "/${var.proyecto}/imagen-uri"
  description = "Imagen de ECR que debe correr la instancia, con su etiqueta"
  type        = "String"
  value       = "${aws_ecr_repository.app.repository_url}:latest"

  lifecycle {
    ignore_changes = [value]
  }

  tags = {
    Name = "${var.proyecto}-imagen-uri"
  }
}

# ===== Dominio =====
# Lo lee el Caddyfile ({$DOMINIO}) para decidir para qué nombre pide el
# certificado de Let's Encrypt.
#
# Mientras esté vacío, se usa la IP pública y Caddy sirve por HTTP sin
# certificado. Al registrar el dominio en Cloudflare basta con actualizar este
# parámetro y redesplegar.
resource "aws_ssm_parameter" "dominio" {
  name        = "/${var.proyecto}/dominio"
  description = "Dominio publico de la app. Vacio mientras no este registrado."
  type        = "String"
  value       = var.dominio != "" ? var.dominio : ":80"

  # ":80" hace que Caddy sirva por HTTP en el puerto 80 sin intentar emitir
  # certificado. Sin esto, Caddy fallaría al arrancar buscando un dominio válido.

  lifecycle {
    ignore_changes = [value]
  }

  tags = {
    Name = "${var.proyecto}-dominio"
  }
}
