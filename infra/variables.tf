# Variables de entrada de la infraestructura (ADR-09).
#
# Centralizar los valores aquí evita repetirlos por todos los archivos: cambiar
# el tamaño de la instancia es editar UNA línea, no buscar en cinco sitios.
#
# Los `validation` no son adorno: fallan en `terraform plan`, ANTES de crear
# nada, en vez de fallar a mitad de un `apply` con recursos ya creados a medias.

variable "region" {
  description = "Región de AWS donde vive toda la infraestructura."
  type        = string
  default     = "us-east-1"

  # us-east-1 es la región más barata y la que tiene todos los servicios. Al ser
  # un proyecto académico de un solo usuario, la latencia desde México (~50 ms)
  # es irrelevante frente al ahorro.
}

variable "ambiente" {
  description = "Nombre del ambiente. Se usa como etiqueta y como prefijo de nombres."
  type        = string
  default     = "prod"
}

variable "proyecto" {
  description = "Prefijo para nombrar todos los recursos, para localizarlos fácil en la consola."
  type        = string
  default     = "gymtracker"
}

# ===== Red =====

variable "vpc_cidr" {
  description = "Rango de IPs privadas de la VPC."
  type        = string
  default     = "10.0.0.0/16"

  # /16 = 65,536 direcciones. Es enormemente más de lo que necesitamos, pero el
  # rango es privado y no cuesta nada: no se puede ampliar cómodamente después,
  # así que se elige holgado desde el principio.
}

# ===== Cómputo =====

variable "tipo_instancia" {
  description = "Tamaño de la EC2 que corre la app y Caddy."
  type        = string
  default     = "t3.small"

  # t3.small = 2 vCPU, 2 GB RAM, ~$15.18/mes.
  # Se descartó t3.micro (1 GB, ~$7.60/mes): .NET con EF Core más el contenedor
  # de Caddy dejan muy poco margen, y quedarse sin memoria durante una
  # demostración en clase cuesta más que los $7.60 ahorrados.
}

variable "tamano_disco_gb" {
  description = "Tamaño del disco EBS de la EC2, en GB."
  type        = number
  default     = 20

  validation {
    condition     = var.tamano_disco_gb >= 20 && var.tamano_disco_gb <= 100
    error_message = "El disco debe estar entre 20 y 100 GB: menos no alcanza para el SO más las imágenes de Docker, y más es gasto innecesario."
  }
}

# ===== Base de datos =====

variable "clase_instancia_db" {
  description = "Tamaño de la instancia de RDS PostgreSQL."
  type        = string
  default     = "db.t4g.micro"

  # db.t4g.micro = 2 vCPU ARM (Graviton), 1 GB RAM, ~$11.68/mes.
  # Las instancias t4g (ARM) cuestan cerca de un 20% menos que las t3 (x86) con
  # rendimiento equivalente. PostgreSQL corre nativo en ARM, así que el ahorro
  # no tiene contrapartida.
}

variable "almacenamiento_db_gb" {
  description = "Almacenamiento asignado a RDS, en GB."
  type        = number
  default     = 20

  # 20 GB es el mínimo de RDS para gp3. La base de GymTracker (1324 ejercicios de
  # catálogo más los datos del usuario) usa muy por debajo de 1 GB.
}

variable "version_postgres" {
  description = "Versión mayor de PostgreSQL, la misma que el contenedor de desarrollo."
  type        = string
  default     = "16"

  # Debe coincidir con docker-compose.yml (postgres:16). Una versión distinta en
  # producción podría comportarse diferente ante las mismas migraciones de EF Core.
}

variable "usuario_db" {
  description = "Usuario administrador de PostgreSQL en RDS."
  type        = string
  default     = "gymtracker_user"

  # El mismo que en desarrollo, para que la connection string sólo cambie de host.
}

variable "db_password" {
  description = "Contraseña de PostgreSQL. NO tiene valor por defecto: se pide al ejecutar."
  type        = string
  sensitive   = true

  # `sensitive = true` impide que Terraform la imprima en la salida de plan/apply.
  # Al no tener default, Terraform la pide por teclado y no queda en ningún
  # archivo del repositorio. NUNCA se escribe aquí ni en un .tfvars versionado.
  #
  # ADVERTENCIA: aun así queda en texto plano DENTRO del estado de Terraform. Por
  # eso el bucket de S3 está cifrado y con el acceso público bloqueado.

  validation {
    condition     = length(var.db_password) >= 16
    error_message = "La contraseña debe tener al menos 16 caracteres: la base de datos guarda datos personales de salud."
  }

  validation {
    # RDS rechaza estos caracteres en la contraseña del usuario maestro. Vale más
    # descubrirlo aquí que a los 10 minutos de esperar a que se cree la instancia.
    condition     = !can(regex("[/@\"' ]", var.db_password))
    error_message = "La contraseña no puede contener / @ \" ' ni espacios: RDS los rechaza."
  }
}

# ===== Despliegue continuo =====

variable "repositorio_github" {
  description = "Repositorio en formato usuario/repo. Acota qué puede asumir el rol OIDC."
  type        = string
  default     = "Fernando-Castro-Hernandez/GymTracker"

  # Este valor es la frontera de seguridad del rol de GitHub Actions: el rol sólo
  # confía en tokens emitidos para ESTE repositorio y desde la rama main. Si
  # alguien copiara el workflow a otro repositorio, su token declararía otro
  # `repo:` y AWS rechazaría la petición.

  validation {
    condition     = can(regex("^[^/]+/[^/]+$", var.repositorio_github))
    error_message = "Debe tener el formato usuario/repositorio, sin la URL completa."
  }
}

# ===== Aplicación =====

variable "dominio" {
  description = "Dominio público de la app. Vacío mientras no esté registrado."
  type        = string
  default     = ""

  # El dominio se registrará en Cloudflare, no en Route 53, porque la cuenta de
  # AWS es nueva y su filtro antifraude bloqueó el registro. Cloudflare incluye
  # DNS gratuito, así que NO hace falta una zona alojada de Route 53 (ahorra
  # $0.50/mes) ni un archivo dns.tf.
  #
  # Mientras esté vacío, Caddy sirve por la IP pública sin certificado válido.
  # Al registrarlo: crear un registro A que apunte a la IP elástica, CON EL PROXY
  # DE CLOUDFLARE DESACTIVADO (nube gris, "DNS only"). Con el proxy activo, el
  # desafío ACME no llega al servidor y Caddy no puede emitir el certificado.
}
