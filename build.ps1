$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  throw 'Missing .NET SDK. Install .NET 8 SDK (not only the Desktop Runtime).'
}
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
  throw 'Missing Node.js 20+ for the AES70 bridge.'
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
  throw 'Missing npm. Install Node.js with npm.'
}

Push-Location (Join-Path $PSScriptRoot 'bridge')
try {
  if (-not (Test-Path 'node_modules/aes70')) {
    npm install --omit=dev --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'npm install failed.' }
  }
}
finally { Pop-Location }

$output = Join-Path $PSScriptRoot 'dist/ScarlettMonitorOSD'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$project = Join-Path $PSScriptRoot 'src/ScarlettMonitorOSD.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:PublishTrimmed=false -o $output
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# Ensure JS bridge and installed dependencies are alongside executable.
Copy-Item (Join-Path $PSScriptRoot 'bridge') -Destination $output -Recurse -Force
$nodeBinary = (Get-Command node).Source
Copy-Item $nodeBinary (Join-Path $output 'node.exe') -Force
Copy-Item (Join-Path $PSScriptRoot 'THIRD_PARTY_NOTICES.md') $output -Force
Write-Host "Built: $output" -ForegroundColor Green
