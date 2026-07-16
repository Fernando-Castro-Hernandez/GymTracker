# tools/generate-seed.ps1
# Genera SeedData/exercises.json descargando el catalogo completo de ExerciseDB OSS
# UNA sola vez, con paginacion paciente y backoff ante HTTP 429.
#
# Se corre manualmente en desarrollo (no en runtime). El runtime lee el JSON local.
#   pwsh/powershell -File tools/generate-seed.ps1
#
# Regla de oro: solo escribe el archivo si el catalogo vino COMPLETO
# (meta.hasNextPage == false y el conteo coincide con meta.total).

# Nota: NO forzar SecurityProtocol a Tls12-only; el servidor negocia Tls13 y
# forzar Tls12 rompe el handshake en .NET Framework. El default (SystemDefault) funciona.

$Base       = 'https://oss.exercisedb.dev/api/v1/exercises'
$PageLimit  = 25            # tope duro de la API
$DelayMs    = 1500          # pausa entre paginas (evita el 429 por rafaga)
$MaxRetries = 6             # reintentos ante 429 por pagina

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutDir     = Join-Path (Split-Path -Parent $ScriptDir) 'SeedData'
$OutFile    = Join-Path $OutDir 'exercises.json'

$all      = [System.Collections.Generic.List[object]]::new()
$cursor   = $null
$total    = $null
$hasNext  = $true
$page     = 0

Write-Host "Descargando catalogo de ExerciseDB OSS (paginas de $PageLimit)..."

while ($hasNext) {
    $page++
    $url = "${Base}?limit=$PageLimit"
    if ($cursor) { $url += "&after=${cursor}" }

    $resp = $null
    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
        try {
            $resp = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 30
            break
        }
        catch {
            $code = $null
            if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
            if ($code -eq 429) {
                $backoff = 1.5 * $attempt
                Write-Host ("  pagina {0}: 429, backoff {1}s (intento {2}/{3})" -f $page, $backoff, $attempt, $MaxRetries)
                Start-Sleep -Seconds $backoff
                continue
            }
            throw
        }
    }

    if ($null -eq $resp) {
        Write-Error "La pagina $page fallo tras $MaxRetries intentos. Abortando SIN escribir el archivo."
        exit 1
    }

    foreach ($item in $resp.data) { $all.Add($item) }
    $total   = $resp.meta.total
    $cursor  = $resp.meta.nextCursor
    $hasNext = [bool]$resp.meta.hasNextPage

    Write-Host ("  pagina {0}: +{1}  (acumulado {2}/{3})" -f $page, $resp.data.Count, $all.Count, $total)

    if ($hasNext) { Start-Sleep -Milliseconds $DelayMs }
}

Write-Host ""
Write-Host ("Descarga terminada: {0} ejercicios (meta.total={1})." -f $all.Count, $total)

# Salvaguarda: no escribir si quedo incompleto.
if ($hasNext) {
    Write-Error "El catalogo quedo incompleto (hasNextPage sigue true). NO se escribe el archivo."
    exit 1
}
if ($total -and $all.Count -lt $total) {
    Write-Error ("Conteo ({0}) menor que meta.total ({1}). NO se escribe el archivo." -f $all.Count, $total)
    exit 1
}

# ===== Verificacion de GIFs =====
# El dataset lista un gifUrl para cada ejercicio, pero ~13% dan 404 en el CDN.
# Como el catalogo existe para la demostracion visual, dejamos SOLO los ejercicios
# cuyo GIF realmente existe (HTTP 200). Se comprueba con HEAD concurrente por lotes
# (el CDN no tiene rate limit), asi la verificacion tarda segundos, no minutos.
Write-Host ""
Write-Host "Verificando que cada GIF exista en el CDN (HEAD concurrente)..."

Add-Type -AssemblyName System.Net.Http
$http = [System.Net.Http.HttpClient]::new()
$http.Timeout = [TimeSpan]::FromSeconds(20)

$conGif    = [System.Collections.Generic.List[object]]::new()
$BatchSize = 40

for ($i = 0; $i -lt $all.Count; $i += $BatchSize) {
    $fin   = [Math]::Min($i + $BatchSize - 1, $all.Count - 1)
    $batch = $all[$i..$fin]

    $pendientes = foreach ($e in $batch) {
        $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Head, $e.gifUrl)
        [PSCustomObject]@{ Ex = $e; Task = $http.SendAsync($req) }
    }

    foreach ($p in $pendientes) {
        try {
            $resp = $p.Task.GetAwaiter().GetResult()
            if ($resp.IsSuccessStatusCode) { $conGif.Add($p.Ex) }
            $resp.Dispose()
        } catch { }
    }

    Write-Host ("  verificados {0}/{1}  (con GIF: {2})" -f ($fin + 1), $all.Count, $conGif.Count)
}

$http.Dispose()

$descartados = $all.Count - $conGif.Count
Write-Host ""
Write-Host ("Verificacion: {0} con GIF valido, {1} descartados (GIF 404)." -f $conGif.Count, $descartados)

if ($conGif.Count -lt ($all.Count * 0.5)) {
    Write-Error "Mas de la mitad de los GIFs fallaron; algo anda mal. NO se escribe el archivo."
    exit 1
}

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
$conGif | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutFile -Encoding utf8

$size = [math]::Round((Get-Item $OutFile).Length / 1KB, 0)
Write-Host ("Escrito: {0} ({1} KB, {2} ejercicios con GIF)." -f $OutFile, $size, $conGif.Count)
