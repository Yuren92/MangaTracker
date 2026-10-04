<#
.SYNOPSIS
    Deploys the API to MonsterASP: database migrations, publish, health check.

.DESCRIPTION
    Reads the credentials from local files that are never committed:
      - src/MangaTracker.Api/appsettings.Production.json (connection string)
      - deploy/private/*.publishSettings (Web Deploy profile from the MonsterASP panel)

    Steps, in this order so the database is ready before the new code runs:
      1. Apply deploy/manga-tracker-migrations.sql (idempotent: safe to run twice).
      2. dotnet publish in Release, without Development settings.
      3. Web Deploy to the site, taking it offline during the copy (app_offline.htm).
      4. GET /health until the API answers Healthy.

    Use -WhatIf to see what Web Deploy would change without changing anything
    (the database step is skipped in that mode).

    Requirements: sqlcmd and Web Deploy 3 (msdeploy.exe).
#>
[CmdletBinding()]
param(
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $root 'src\MangaTracker.Api'
$settingsFile = Join-Path $apiProject 'appsettings.Production.json'
$migrationScript = Join-Path $root 'deploy\manga-tracker-migrations.sql'
$publishDir = Join-Path $root 'deploy\private\publish'
$msdeploy = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe'
$healthUrl = 'https://mangatracker.runasp.net/health'

function Step([string]$text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

# --- Inputs -------------------------------------------------------------------

if (-not (Test-Path $settingsFile)) { throw "Missing $settingsFile" }
if (-not (Test-Path $msdeploy)) { throw "Web Deploy not found at $msdeploy" }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw 'sqlcmd not found' }

$profileFile = Get-ChildItem (Join-Path $root 'deploy\private') -Filter '*.publishSettings' | Select-Object -First 1
if (-not $profileFile) { throw 'No .publishSettings file in deploy\private' }

[xml]$profileXml = Get-Content $profileFile.FullName
$webDeploy = $profileXml.publishData.publishProfile | Where-Object { $_.publishMethod -eq 'MSDeploy' } | Select-Object -First 1

$settings = Get-Content $settingsFile -Raw | ConvertFrom-Json
$connection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($settings.ConnectionStrings.MangaTrackerDb)

# --- 1. Database --------------------------------------------------------------

if ($WhatIf) {
    Step 'Database: skipped (-WhatIf)'
} else {
    Step "Database: applying migrations to $($connection.InitialCatalog) on $($connection.DataSource)"
    $sqlArgs = @('-S', $connection.DataSource, '-d', $connection.InitialCatalog,
                 '-U', $connection.UserID, '-P', $connection.Password,
                 '-i', $migrationScript, '-b')
    if ($connection.Encrypt) { $sqlArgs += '-N' }
    if ($connection.TrustServerCertificate) { $sqlArgs += '-C' }
    & sqlcmd @sqlArgs | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Migrations failed (sqlcmd exit code $LASTEXITCODE)" }
    Write-Host 'Migrations applied.'
}

# --- 2. Publish ---------------------------------------------------------------

Step 'Publishing the API (Release)'
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $apiProject -c Release -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

# Development settings (local database, local keys) never go to the server.
Get-ChildItem $publishDir -Filter 'appsettings.Development*.json' | Remove-Item -Force

# --- 3. Web Deploy ------------------------------------------------------------

Step "Deploying to $($webDeploy.msdeploySite) via $($webDeploy.publishUrl)"
$destination = "contentPath='$($webDeploy.msdeploySite)'," +
               "computerName='https://$($webDeploy.publishUrl):8172/msdeploy.axd?site=$($webDeploy.msdeploySite)'," +
               "userName='$($webDeploy.userName)',password='$($webDeploy.userPWD)',authType='Basic'"

$deployArgs = @(
    '-verb:sync',
    "-source:contentPath='$publishDir'",
    "-dest:$destination",
    '-enableRule:AppOffline',
    '-retryAttempts:3',
    '-allowUntrusted'
)
if ($WhatIf) { $deployArgs += '-whatif' }

& $msdeploy @deployArgs
if ($LASTEXITCODE -ne 0) { throw "Web Deploy failed (exit code $LASTEXITCODE)" }

if ($WhatIf) {
    Step 'Dry run finished: nothing was changed'
    return
}

# --- 4. Health check ----------------------------------------------------------

Step "Waiting for $healthUrl"
for ($attempt = 1; $attempt -le 20; $attempt++) {
    try {
        $response = Invoke-WebRequest $healthUrl -UseBasicParsing -TimeoutSec 15
        if ($response.Content -eq 'Healthy') {
            Write-Host "Healthy after $attempt attempt(s)." -ForegroundColor Green
            return
        }
    } catch {
        Start-Sleep -Seconds 5
    }
}

throw "The API did not report Healthy. Check the MonsterASP logs."
