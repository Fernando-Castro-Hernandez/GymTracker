# Red: VPC, subredes y enrutamiento (ADR-09).
#
# Aquí se decide la seguridad de la base de datos. No la decide la contraseña:
# la decide el hecho de que RDS viva en una subred SIN ruta hacia internet.
# Aunque alguien conociera la contraseña, no habría camino de red para llegar.
#
# Esta es la diferencia real frente al desarrollo local, donde el contenedor de
# PostgreSQL escucha en el puerto 5433 de la máquina.

# ===== Zonas de disponibilidad =====
# Una AZ es un centro de datos físicamente separado dentro de la región. Se
# consultan en vez de escribirlas a mano ("us-east-1a") porque AWS asigna letras
# distintas a cada cuenta: la "us-east-1a" de esta cuenta puede ser un edificio
# diferente al de otra.
data "aws_availability_zones" "disponibles" {
  state = "available"
}

# ===== VPC =====
resource "aws_vpc" "principal" {
  cidr_block = var.vpc_cidr

  # Necesarios para que RDS sea alcanzable por nombre DNS interno en vez de por
  # IP. La connection string apuntará a un host como
  # gymtracker-db.xxxx.us-east-1.rds.amazonaws.com, que sigue funcionando aunque
  # AWS reemplace la instancia y cambie su IP.
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = {
    Name = "${var.proyecto}-vpc"
  }
}

# ===== Internet Gateway =====
# La puerta entre la VPC e internet. Por sí solo no expone nada: un recurso sólo
# alcanza internet si SU tabla de ruteo tiene una ruta hacia aquí. Ese detalle es
# justamente lo que separa a la subred pública de las privadas.
#
# No tiene costo por hora (a diferencia del NAT Gateway).
resource "aws_internet_gateway" "principal" {
  vpc_id = aws_vpc.principal.id

  tags = {
    Name = "${var.proyecto}-igw"
  }
}

# ==========================================================================
# Subred PÚBLICA — la EC2 con la app y Caddy
# ==========================================================================
resource "aws_subnet" "publica" {
  vpc_id                  = aws_vpc.principal.id
  cidr_block              = "10.0.1.0/24" # 256 direcciones, de sobra para 1 instancia
  availability_zone       = data.aws_availability_zones.disponibles.names[0]

  # Asigna IP pública automáticamente a lo que se lance aquí. La EC2 necesita
  # salida a internet para descargar la imagen de ECR y para que Caddy complete
  # el desafío ACME de Let's Encrypt.
  map_public_ip_on_launch = true

  tags = {
    Name = "${var.proyecto}-subred-publica"
    Tipo = "publica"
  }
}

# ==========================================================================
# Subredes PRIVADAS — RDS
# ==========================================================================
# Se crean DOS aunque RDS sólo use una, porque AWS lo EXIGE: el grupo de subredes
# de RDS debe abarcar al menos dos zonas de disponibilidad, incluso en modo
# Single-AZ. Es su requisito para poder recuperar la base si un centro de datos
# falla. La segunda queda vacía y no cuesta nada: las subredes son gratis.

resource "aws_subnet" "privada_a" {
  vpc_id            = aws_vpc.principal.id
  cidr_block        = "10.0.10.0/24"
  availability_zone = data.aws_availability_zones.disponibles.names[0]

  # Sin IP pública. Aquí vive la base de datos.
  map_public_ip_on_launch = false

  tags = {
    Name = "${var.proyecto}-subred-privada-a"
    Tipo = "privada"
  }
}

resource "aws_subnet" "privada_b" {
  vpc_id            = aws_vpc.principal.id
  cidr_block        = "10.0.11.0/24"
  availability_zone = data.aws_availability_zones.disponibles.names[1]

  map_public_ip_on_launch = false

  tags = {
    Name = "${var.proyecto}-subred-privada-b"
    Tipo = "privada"
  }
}

# ==========================================================================
# Enrutamiento
# ==========================================================================

# Tabla de la subred PÚBLICA: todo lo que no sea tráfico interno de la VPC
# (0.0.0.0/0 = "cualquier destino") sale por el internet gateway.
# ESTA RUTA es lo único que hace "pública" a una subred.
resource "aws_route_table" "publica" {
  vpc_id = aws_vpc.principal.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.principal.id
  }

  tags = {
    Name = "${var.proyecto}-rt-publica"
  }
}

resource "aws_route_table_association" "publica" {
  subnet_id      = aws_subnet.publica.id
  route_table_id = aws_route_table.publica.id
}

# Tabla de las subredes PRIVADAS: fíjate en que NO tiene ningún bloque `route`.
# Sólo existe la ruta implícita del rango de la VPC (10.0.0.0/16), que Terraform
# no muestra porque AWS la crea sola. Traducción: lo que viva aquí puede hablar
# con la EC2, y con NADA fuera de la VPC.
#
# DECISIÓN DE COSTO: aquí es donde iría un NAT Gateway si las subredes privadas
# necesitaran salir a internet. Cuesta ~$33/mes, casi tanto como el resto de la
# infraestructura junta. No se pone porque RDS no necesita salida: sólo recibe
# conexiones de la EC2, y AWS le aplica los parches desde su propia red interna.
resource "aws_route_table" "privada" {
  vpc_id = aws_vpc.principal.id

  tags = {
    Name = "${var.proyecto}-rt-privada"
  }
}

resource "aws_route_table_association" "privada_a" {
  subnet_id      = aws_subnet.privada_a.id
  route_table_id = aws_route_table.privada.id
}

resource "aws_route_table_association" "privada_b" {
  subnet_id      = aws_subnet.privada_b.id
  route_table_id = aws_route_table.privada.id
}
