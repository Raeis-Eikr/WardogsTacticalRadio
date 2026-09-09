$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'WardogsTacticalRadio\WardogsTacticalRadio.csproj'

if (-not (Test-Path $project)) {
    throw "Project file not found: $project"
}

[xml]$projectXml = Get-Content $project
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'No <Version> value was found in WardogsTacticalRadio.csproj.'
}

$packageName = "WardogsRadio_v${version}_Windows_x64"
$publishRoot = Join-Path $PSScriptRoot 'publish'
$publishDir = Join-Path $publishRoot $packageName
$zipPath = Join-Path $PSScriptRoot "$packageName.zip"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "Building tester package for v$version..." -ForegroundColor Cyan

dotnet publish $project `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$testingNotes = Join-Path $PSScriptRoot 'TESTING_NOTES.txt'
if (Test-Path $testingNotes) {
    Copy-Item $testingNotes (Join-Path $publishDir 'TESTING_NOTES.txt') -Force
}

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host ''
Write-Host 'Tester package created:' -ForegroundColor Green
Write-Host $zipPath
