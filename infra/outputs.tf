# Valores que Terraform expone al terminar (ADR-09).
#
# Son los datos que hacen falta DESPUÉS de crear la infraestructura: la dirección
# de la base, la URI del registro de imágenes, el ARN del rol de despliegue.
# Se consultan con `terraform output` sin entrar a la consola de AWS.

output "ecr_uri" {
  description = "URI del repositorio de ECR. La usa el pipeline de CD para publicar la imagen."
  value       = aws_ecr_repository.app.repository_url
}

output "rol_github_actions_arn" {
  description = "ARN del rol OIDC. Va en el workflow, en role-to-assume. NO es un secreto."
  value       = aws_iam_role.github_actions.arn

  # No hace falta guardarlo en los Secrets de GitHub: sin un token válido emitido
  # para este repositorio y esta rama, conocer el ARN no sirve de nada.
}

output "rds_endpoint" {
  description = "Host y puerto de la base. Sólo alcanzable desde dentro de la VPC."
  value       = aws_db_instance.principal.endpoint
}

output "rds_direccion" {
  description = "Sólo el host de RDS, sin el puerto. Para armar la connection string."
  value       = aws_db_instance.principal.address
}

# La connection string completa, ya formada, lista para guardarse en SSM
# Parameter Store como SecureString.
#
# `sensitive = true` impide que Terraform la imprima en la salida de apply. Para
# verla hay que pedirla a propósito:
#   terraform output -raw connection_string
output "connection_string" {
  description = "Connection string de Npgsql para ConnectionStrings__DefaultConnection."
  value       = "Host=${aws_db_instance.principal.address};Port=${aws_db_instance.principal.port};Database=${aws_db_instance.principal.db_name};Username=${var.usuario_db};Password=${var.db_password}"
  sensitive   = true
}

output "vpc_id" {
  description = "ID de la VPC, por si hace falta para diagnosticar."
  value       = aws_vpc.principal.id
}

# ===== Cómputo =====

output "ip_publica" {
  description = "IP elástica de la app. A esta dirección apunta el registro A de Cloudflare."
  value       = aws_eip.app.public_ip
}

output "instancia_id" {
  description = "ID de la EC2. Lo usa el pipeline de CD en aws ssm send-command."
  value       = aws_instance.app.id
}

output "url_temporal" {
  description = "Direccion para probar antes de tener dominio."
  value       = "http://${aws_eip.app.public_ip}"
}

# Comando listo para copiar: abre una terminal en la instancia SIN SSH, sin
# puerto 22 abierto y sin llave privada.
output "comando_conectar" {
  description = "Como entrar a la instancia por SSM Session Manager."
  value       = "aws ssm start-session --target ${aws_instance.app.id} --region ${var.region}"
}
