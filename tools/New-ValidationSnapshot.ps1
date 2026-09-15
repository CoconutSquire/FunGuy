param([string]$Name = (Get-Date -Format 'yyyyMMdd-HHmmss'))
$ErrorActionPreference = 'Stop'
if ($Name -notmatch '^[A-Za-z0-9_-]+$') { throw 'Snapshot name may contain only letters, digits, underscores and hyphens.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
$snapshot = Join-Path $repoRoot "artifacts/validation/$Name"
if (Test-Path -LiteralPath $snapshot) { throw "Snapshot already exists: $snapshot" }
$paths = @(& git -C $repoRoot -c core.quotepath=false ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate source files.' }
$manifest = @()
foreach ($relative in $paths) {
    $source = Join-Path $repoRoot $relative
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { continue } # Exclude intentional tracked deletions.
    $destination = Join-Path $snapshot $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
    $manifest += [PSCustomObject]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash }
}
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $snapshot 'snapshot-manifest.json')
Write-Output "Source snapshot: $snapshot"
Write-Output "Unity project: $(Join-Path $snapshot "FunGuy's")"
