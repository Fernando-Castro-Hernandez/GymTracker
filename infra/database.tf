# RDS PostgreSQL — la base de datos gestionada (ADR-09).
#
# Reemplaza al contenedor `gymtracker-db` del docker-compose.yml de desarrollo.
# Lo que se gana al pagar por un servicio gestionado en vez de correr PostgreSQL
# en la misma EC2:
#   · Backups automáticos diarios con recuperación a un punto en el tiempo.
#   · Parches del motor aplicados por AWS en la ventana de mantenimiento.
#   · Aislamiento de red real: vive en una subred sin ruta a internet.
#   · Si la EC2 muere o se recrea, los datos no están dentro de ella.
#
# ⚠️ PRIMER RECURSO QUE COBRA: ~$14/mes (instancia + almacenamiento). Todo lo
# creado hasta aquí (VPC, security groups, IAM, ECR vacío) es gratis.

# ===== Grupo de subredes =====
# Le dice a RDS en qué subredes puede colocar la instancia. AWS EXIGE al menos
# dos zonas de disponibilidad, incluso en Single-AZ, para poder reubicar la base
# si un centro de datos falla. Por eso network.tf crea dos subredes privadas
# aunque sólo se use una.
resource "aws_db_subnet_group" "principal" {
  name        = "${var.proyecto}-subredes-db"
  description = "Subredes privadas donde puede vivir RDS"
  subnet_ids  = [aws_subnet.privada_a.id, aws_subnet.privada_b.id]

  tags = {
    Name = "${var.proyecto}-subredes-db"
  }
}

# ===== Grupo de parámetros =====
# El equivalente al postgresql.conf. Se crea uno propio en vez de usar el que
# viene por defecto porque el de AWS NO se puede modificar: si más adelante hace
# falta ajustar cualquier valor, ya existe el sitio donde hacerlo.
resource "aws_db_parameter_group" "principal" {
  name        = "${var.proyecto}-parametros-pg${var.version_postgres}"
  family      = "postgres${var.version_postgres}"
  description = "Parametros de PostgreSQL para GymTracker"

  # Registra las conexiones. Útil para confirmar que la app se conecta desde la
  # EC2 y detectar intentos desde otro origen.
  parameter {
    name  = "log_connections"
    value = "1"
  }

  # Registra las consultas que tardan más de 1 segundo. Sirve para diagnosticar
  # las gráficas de progreso, que agregan sesiones y series.
  parameter {
    name  = "log_min_duration_statement"
    value = "1000"
  }

  lifecycle {
    create_before_destroy = true
  }
}

# ===== La instancia =====
resource "aws_db_instance" "principal" {
  identifier = "${var.proyecto}-db"

  # --- Motor ---
  engine         = "postgres"
  engine_version = var.version_postgres
  instance_class = var.clase_instancia_db

  # Las instancias t4g usan procesadores ARM (Graviton) de AWS: ~20% más baratas
  # que las t3 (x86) con rendimiento equivalente, y PostgreSQL corre nativo en
  # ARM. Ahorro sin contrapartida.
  #
  # OJO: esto NO aplica a la EC2, que será x86 (t3.small), porque la imagen
  # Docker de la app se construye para linux/amd64 y no correría en ARM sin
  # recompilarla.

  # --- Almacenamiento ---
  allocated_storage = var.almacenamiento_db_gb
  storage_type      = "gp3"
  storage_encrypted = true # Cifrado en reposo. Sin costo adicional.

  # Crece solo hasta 100 GB si se llena, en vez de fallar. Sólo se paga lo usado.
  max_allocated_storage = 100

  # --- Credenciales ---
  db_name  = "gymtracker" # Mismo nombre que en desarrollo
  username = var.usuario_db
  password = var.db_password

  # ⚠️ La contraseña queda EN TEXTO PLANO dentro del estado de Terraform. Es una
  # limitación conocida de la herramienta, no un descuido: por eso el bucket de
  # S3 está cifrado con AES-256 y tiene bloqueado todo acceso público.

  # --- Red: aquí está el aislamiento ---
  db_subnet_group_name   = aws_db_subnet_group.principal.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  # SIN IP PÚBLICA. Combinado con la subred sin ruta a internet (network.tf) y el
  # security group que sólo acepta el 5432 desde el SG de la EC2 (security.tf),
  # son tres capas independientes sobre el mismo principio: la base de datos no
  # es alcanzable desde fuera de la VPC.
  publicly_accessible = false

  parameter_group_name = aws_db_parameter_group.principal.name

  # --- Backups ---
  # 7 días de retención permiten recuperación a un punto en el tiempo: se puede
  # restaurar la base tal como estaba en cualquier segundo de esos 7 días.
  # El almacenamiento de backups es gratis hasta el tamaño de la base.
  backup_retention_period = 7

  # Horarios en UTC, elegidos de madrugada en hora de México (UTC-6):
  #   06:00-07:00 UTC = 00:00-01:00 en México
  backup_window      = "06:00-07:00"
  maintenance_window = "Mon:07:00-Mon:08:00"

  # --- Alta disponibilidad ---
  # Single-AZ a propósito. Multi-AZ mantiene una réplica en otro centro de datos
  # con conmutación automática, pero cuesta EXACTAMENTE EL DOBLE (~$23/mes en vez
  # de $11.68). Para una app académica de un usuario, pagar el doble por evitar
  # unos minutos de caída improbable no se justifica. Los backups diarios cubren
  # el riesgo que sí importa: perder datos.
  multi_az = false

  # --- Actualizaciones ---
  # Parches menores automáticos (16.4 → 16.5): correcciones de seguridad sin
  # cambios incompatibles. Los saltos mayores (16 → 17) siguen siendo manuales,
  # porque podrían alterar el comportamiento de las migraciones de EF Core.
  auto_minor_version_upgrade = true

  # --- Al destruir ---
  # `terraform destroy` toma una foto completa de la base ANTES de borrarla, y
  # desde ella se puede restaurar al recrear la infraestructura. ESTO ES LO QUE
  # HACE SEGURO apagar todo entre demostraciones para no pagar: sin el snapshot,
  # destroy borraría los datos para siempre.
  skip_final_snapshot       = false
  final_snapshot_identifier = "${var.proyecto}-snapshot-final"

  # Protección contra borrado accidental por la consola o la CLI.
  #
  # Queda en false a propósito: en true, `terraform destroy` FALLA y habría que
  # desactivarla a mano antes. Como apagar la infraestructura entre demos es
  # parte del plan de costos, esa protección estorbaría más de lo que aporta. El
  # final_snapshot ya cubre la pérdida de datos.
  deletion_protection = false

  # --- Observabilidad ---
  # Envía los logs de PostgreSQL a CloudWatch. Sin esto sólo se ven desde la
  # consola de RDS y con retención limitada.
  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]

  tags = {
    Name = "${var.proyecto}-db"
  }
}
