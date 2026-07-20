# Identidades y permisos (ADR-09).
#
# La idea central: NINGUNA credencial de AWS escrita en disco, ni en el servidor
# ni en los secretos de GitHub.
#
# Un USUARIO IAM tiene llaves fijas que no expiran y viven en un archivo. Un ROL
# es una identidad que AWS PRESTA temporalmente: las credenciales llegan por
# memoria, duran unas horas y se renuevan solas. Si se filtran, caducan.
#
# Aquí se definen dos roles:
#   1. Rol de instancia — la EC2 lee ECR y SSM sin credenciales en disco.
#   2. Rol OIDC — GitHub Actions despliega sin guardar una AWS_ACCESS_KEY.

# Datos de la cuenta y la región actuales, para construir ARNs sin escribir el
# número de cuenta a mano en cada política.
data "aws_caller_identity" "actual" {}
data "aws_region" "actual" {}

# ==========================================================================
# ROL 1 — La instancia EC2
# ==========================================================================

# La "trust policy" responde a QUIÉN puede asumir este rol. Aquí: el servicio
# EC2. Es distinto de los permisos, que responden a QUÉ puede hacer.
data "aws_iam_policy_document" "ec2_confianza" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ec2" {
  name               = "${var.proyecto}-rol-ec2"
  description        = "Permite a la EC2 leer ECR y SSM sin credenciales en disco"
  assume_role_policy = data.aws_iam_policy_document.ec2_confianza.json

  tags = {
    Name = "${var.proyecto}-rol-ec2"
  }
}

# ===== Permiso: ser administrada por SSM =====
# Política gestionada por AWS. Es LO QUE SUSTITUYE AL SSH: instala el permiso
# para que el agente SSM de la instancia abra un túnel SALIENTE hacia AWS, por el
# que después viaja la terminal.
#
# Por eso el puerto 22 puede quedar cerrado en security.tf: la conexión la inicia
# la instancia hacia fuera, no al revés.
resource "aws_iam_role_policy_attachment" "ec2_ssm" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# ===== Permiso: descargar imágenes de ECR =====
# Escrito a mano en vez de usar la política gestionada de AWS
# (AmazonEC2ContainerRegistryReadOnly), que da lectura sobre TODOS los
# repositorios de la cuenta. Esta sólo permite el repositorio de GymTracker.
data "aws_iam_policy_document" "ec2_ecr" {
  # GetAuthorizationToken no admite acotarse por recurso: es la llamada que
  # obtiene el token de login del registro, y AWS sólo acepta "*". Es una
  # limitación de la API, no un descuido.
  statement {
    sid       = "ObtenerTokenDeRegistro"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  # La descarga en sí SÍ se acota al repositorio de este proyecto.
  statement {
    sid = "DescargarImagenesDeGymTracker"
    actions = [
      "ecr:BatchGetImage",
      "ecr:GetDownloadUrlForLayer",
      "ecr:BatchCheckLayerAvailability"
    ]
    resources = [aws_ecr_repository.app.arn]
  }
}

resource "aws_iam_role_policy" "ec2_ecr" {
  name   = "${var.proyecto}-ec2-ecr"
  role   = aws_iam_role.ec2.id
  policy = data.aws_iam_policy_document.ec2_ecr.json
}

# ===== Permiso: leer los secretos de SSM Parameter Store =====
# Aquí viven la connection string de RDS y las API keys de Anthropic y Gemini.
# Es el reemplazo en producción de User Secrets (§7.3 de PLAN-integraciones-IA).
data "aws_iam_policy_document" "ec2_ssm_parametros" {
  statement {
    sid = "LeerParametrosDeGymTracker"
    actions = [
      "ssm:GetParameter",
      "ssm:GetParameters",
      "ssm:GetParametersByPath"
    ]
    # Acotado al prefijo del proyecto: NO puede leer los parámetros de otros
    # sistemas que llegaran a existir en esta cuenta.
    resources = [
      "arn:aws:ssm:${data.aws_region.actual.region}:${data.aws_caller_identity.actual.account_id}:parameter/${var.proyecto}/*"
    ]
  }

  # Los parámetros son de tipo SecureString, cifrados con la llave KMS que AWS
  # gestiona para SSM. Sin permiso de descifrado, la lectura devolvería el valor
  # cifrado y sería inútil.
  statement {
    sid       = "DescifrarLosParametros"
    actions   = ["kms:Decrypt"]
    resources = ["*"]

    # El permiso de KMS se acota por CONDICIÓN en vez de por recurso: sólo sirve
    # cuando el descifrado lo pide SSM. No se puede usar para descifrar otra cosa.
    condition {
      test     = "StringEquals"
      variable = "kms:ViaService"
      values   = ["ssm.${data.aws_region.actual.region}.amazonaws.com"]
    }
  }
}

resource "aws_iam_role_policy" "ec2_ssm_parametros" {
  name   = "${var.proyecto}-ec2-ssm-parametros"
  role   = aws_iam_role.ec2.id
  policy = data.aws_iam_policy_document.ec2_ssm_parametros.json
}

# El "instance profile" es el envoltorio que permite adjuntar un rol a una
# instancia EC2. Es un requisito de la API: un rol no se asocia directamente.
resource "aws_iam_instance_profile" "ec2" {
  name = "${var.proyecto}-perfil-ec2"
  role = aws_iam_role.ec2.name
}

# ==========================================================================
# ROL 2 — GitHub Actions vía OIDC
# ==========================================================================
#
# El problema clásico del despliegue continuo: GitHub Actions necesita permisos
# en AWS, y lo habitual es guardar una AWS_ACCESS_KEY en los Secrets del repo,
# donde queda para siempre y sin expirar.
#
# Con OIDC no se guarda ningún secreto. En cada ejecución:
#   1. GitHub emite un token firmado que declara de qué repositorio y rama viene.
#   2. El workflow se lo presenta a AWS.
#   3. AWS verifica la firma contra las llaves públicas de GitHub y comprueba
#      que el repositorio y la rama coincidan con lo permitido aquí abajo.
#   4. AWS devuelve credenciales temporales de ~1 hora.
#
# La confianza se establece criptográficamente en cada corrida.

# Registra a GitHub como proveedor de identidad de confianza para esta cuenta.
resource "aws_iam_openid_connect_provider" "github" {
  url = "https://token.actions.githubusercontent.com"

  # El "audience": para quién está destinado el token. Confirma que fue emitido
  # para AWS y no para otro servicio.
  client_id_list = ["sts.amazonaws.com"]

  # Desde 2023 AWS valida la cadena de certificados de GitHub por su cuenta, así
  # que ya no hace falta fijar la huella del certificado (que caducaba y rompía
  # los despliegues). Se deja el valor histórico porque el campo sigue siendo
  # obligatorio en la API.
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]

  tags = {
    Name = "${var.proyecto}-oidc-github"
  }
}

data "aws_iam_policy_document" "github_confianza" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }

    # LA CONDICIÓN QUE DE VERDAD PROTEGE.
    #
    # Restringe el rol a ESTE repositorio y SOLO a la rama main. Si alguien
    # copiara el workflow a otro repositorio, su token diría otro `repo:` y AWS
    # rechazaría la petición.
    #
    # Sin esta condición, CUALQUIER repositorio de GitHub en el mundo podría
    # asumir el rol. Es el error de configuración más común con OIDC.
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values   = ["repo:${var.repositorio_github}:ref:refs/heads/main"]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = "${var.proyecto}-rol-github-actions"
  description        = "Despliegue desde GitHub Actions por OIDC, sin llaves guardadas"
  assume_role_policy = data.aws_iam_policy_document.github_confianza.json

  # Las credenciales caducan en 1 hora. Un despliegue tarda pocos minutos, así
  # que no hay razón para darle más margen.
  max_session_duration = 3600

  tags = {
    Name = "${var.proyecto}-rol-github-actions"
  }
}

# ===== Permisos de GitHub Actions: subir a ECR y desplegar por SSM =====
data "aws_iam_policy_document" "github_despliegue" {
  statement {
    sid       = "ObtenerTokenDeRegistro"
    actions   = ["ecr:GetAuthorizationToken"]
    resources = ["*"]
  }

  # Subir imágenes SÓLO al repositorio de GymTracker.
  statement {
    sid = "PublicarImagenEnGymTracker"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:CompleteLayerUpload",
      "ecr:InitiateLayerUpload",
      "ecr:PutImage",
      "ecr:UploadLayerPart",
      # Lectura, para reaprovechar capas ya subidas y no reenviarlas enteras.
      "ecr:BatchGetImage",
      "ecr:GetDownloadUrlForLayer"
    ]
    resources = [aws_ecr_repository.app.arn]
  }

  # Ordenar el redespliegue en la instancia. Reemplaza al `ssh servidor "docker
  # compose pull && up -d"` de un despliegue tradicional: sin puerto 22 abierto y
  # sin llave privada guardada en GitHub.
  #
  statement {
    sid     = "EjecutarElRedespliegueEnLaInstancia"
    actions = ["ssm:SendCommand"]
    resources = [
      # Acotado A ESTA instancia. Con "*" podría ejecutar comandos en cualquier
      # máquina de la cuenta.
      "arn:aws:ec2:${data.aws_region.actual.region}:${data.aws_caller_identity.actual.account_id}:instance/${aws_instance.app.id}",
      # El documento de SSM que ejecuta comandos de shell.
      "arn:aws:ssm:${data.aws_region.actual.region}::document/AWS-RunShellScript"
    ]
  }

  # Consultar si el comando terminó bien. Sólo lectura; no admite acotarse por
  # recurso porque el ID de la invocación no se conoce de antemano.
  statement {
    sid = "ConsultarElResultadoDelComando"
    actions = [
      "ssm:GetCommandInvocation",
      "ssm:ListCommandInvocations"
    ]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "github_despliegue" {
  name   = "${var.proyecto}-github-despliegue"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_despliegue.json
}
