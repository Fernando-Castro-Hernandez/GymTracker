# Security Groups: cortafuegos a nivel de recurso (ADR-09).
#
# Segunda capa de defensa, INDEPENDIENTE del ruteo:
#   · La tabla de ruteo decide si EXISTE CAMINO hacia un destino.
#   · El security group decide QUÉ PUERTOS se aceptan y DE QUIÉN.
#
# Los security groups son "stateful": si se permite una conexión de entrada, la
# respuesta sale automáticamente. No hay que abrir el retorno a mano (las ACL de
# red, que aquí no se usan, sí lo exigirían).

# ==========================================================================
# Security Group de la EC2 — la única puerta a internet
# ==========================================================================
resource "aws_security_group" "ec2" {
  name        = "${var.proyecto}-sg-ec2"
  description = "Permite HTTP y HTTPS desde internet. SSH cerrado a proposito."
  vpc_id      = aws_vpc.principal.id

  tags = {
    Name = "${var.proyecto}-sg-ec2"
  }
}

# Puerto 80 (HTTP). No sirve la app: Caddy lo usa para dos cosas obligatorias.
#   1. Redirigir a HTTPS a quien escriba la dirección sin https://
#   2. Responder el desafío ACME de Let's Encrypt, que SIEMPRE llega por el 80.
# Cerrarlo impediría emitir y renovar el certificado.
resource "aws_vpc_security_group_ingress_rule" "ec2_http" {
  security_group_id = aws_security_group.ec2.id
  # NOTA: sin apostrofes. La API de EC2 rechaza las descripciones que no usen el
  # juego a-zA-Z0-9 y . _-:/()#,@[]+=&;{}!$* — un limite que `terraform plan` no
  # detecta, porque solo lo valida AWS al recibir la peticion.
  description       = "HTTP: redireccion a HTTPS y desafio ACME de Lets Encrypt"

  cidr_ipv4   = "0.0.0.0/0" # Cualquier origen: es un sitio web público
  from_port   = 80
  ip_protocol = "tcp"
  to_port     = 80
}

# Puerto 443 (HTTPS). El tráfico real de la aplicación.
resource "aws_vpc_security_group_ingress_rule" "ec2_https" {
  security_group_id = aws_security_group.ec2.id
  description       = "HTTPS: el trafico real de la aplicacion"

  cidr_ipv4   = "0.0.0.0/0"
  from_port   = 443
  ip_protocol = "tcp"
  to_port     = 443
}

# ===== EL PUERTO 22 (SSH) NO SE ABRE. Es deliberado. =====
#
# El acceso a la instancia será por SSM Session Manager: la EC2 abre una conexión
# SALIENTE hacia AWS y la terminal viaja por ese túnel. Consecuencias concretas:
#   · No existe llave privada que se pueda filtrar, perder o subir a git.
#   · No hay puerto 22 expuesto a los escáneres automáticos de internet.
#   · El acceso se autoriza con IAM y queda registrado en CloudTrail.
#
# Es también lo que evita el archivo .pem que se usa en los cursos de AWS
# Academy, y que es justo el que se termina perdiendo o subiendo a un repositorio.

# Salida: sin restricción. La EC2 necesita alcanzar ECR (imágenes Docker), SSM,
# Let's Encrypt y las APIs de Anthropic y Gemini para el Coach y el chatbot.
# Restringir la salida por IP no funcionaría: esos servicios usan rangos que
# cambian sin aviso.
resource "aws_vpc_security_group_egress_rule" "ec2_salida" {
  security_group_id = aws_security_group.ec2.id
  description       = "Salida libre: ECR, SSM, Lets Encrypt y las APIs de IA"

  cidr_ipv4   = "0.0.0.0/0"
  ip_protocol = "-1" # -1 = todos los protocolos y puertos
}

# ==========================================================================
# Security Group de RDS — sólo habla con la EC2
# ==========================================================================
resource "aws_security_group" "rds" {
  name        = "${var.proyecto}-sg-rds"
  description = "PostgreSQL accesible unicamente desde el security group de la EC2."
  vpc_id      = aws_vpc.principal.id

  tags = {
    Name = "${var.proyecto}-sg-rds"
  }
}

# LA REGLA MÁS IMPORTANTE DE TODA LA INFRAESTRUCTURA.
#
# Fíjate en que NO dice `cidr_ipv4` sino `referenced_security_group_id`: la regla
# no apunta a una DIRECCIÓN, apunta a una IDENTIDAD.
#
# Se traduce como: "acepto el 5432 de cualquier recurso que lleve puesto el
# security group de la EC2", en vez de "acepto el 5432 desde 10.0.1.47".
#
# Dos consecuencias prácticas:
#   · Si la EC2 se reemplaza y cambia de IP, la regla sigue siendo válida.
#   · Si mañana se lanza otra instancia en la MISMA subred sin ese security
#     group, no podrá conectarse a la base de datos.
resource "aws_vpc_security_group_ingress_rule" "rds_desde_ec2" {
  security_group_id = aws_security_group.rds.id
  description       = "PostgreSQL solo desde la EC2, por identidad y no por IP"

  referenced_security_group_id = aws_security_group.ec2.id
  from_port                    = 5432
  ip_protocol                  = "tcp"
  to_port                      = 5432
}

# Salida de RDS: ninguna regla, a propósito.
#
# Un security group sin reglas de salida bloquea TODO el tráfico saliente. La
# base de datos no inicia conexiones: sólo responde, y las respuestas salen por
# el comportamiento stateful sin necesitar una regla.
#
# Es la tercera capa sobre el mismo principio: la subred no tiene ruta a internet
# (network.tf), no hay NAT Gateway, y ahora tampoco hay regla de salida.
