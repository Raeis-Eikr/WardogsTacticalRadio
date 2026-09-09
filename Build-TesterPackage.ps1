$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'WardogsTacticalRadio\WardogsTacticalRadio.csproj'
$mainWindow = Join-Path $PSScriptRoot 'WardogsTacticalRadio\MainWindow.xaml.cs'
$buildNotes = Join-Path $PSScriptRoot 'BUILD_NOTES.md'

if (-not (Test-Path $project)) {
    throw "Project file not found: $project"
}

[xml]$projectXml = Get-Content $project
$version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'No <Version> value was found in WardogsTacticalRadio.csproj.'
}

$uiVersion = "v$version"

# Pre-distribution consistency checks. Fail closed if version metadata disagrees.
if (Test-Path $mainWindow) {
    $mainText = Get-Content $mainWindow -Raw
    if ($mainText -notmatch [regex]::Escape($uiVersion)) {
        throw "Release version mismatch: project reports $version but MainWindow.xaml.cs does not contain $uiVersion."
    }
}

if (Test-Path $buildNotes) {
    $notesText = Get-Content $buildNotes -Raw
    if ($notesText -notmatch [regex]::Escape($uiVersion)) {
        throw "Release version mismatch: project reports $version but BUILD_NOTES.md does not contain $uiVersion."
    }
}

$packageName = "WardogsRadio_v${version}_Windows_x64"
$publishRoot = Join-Path $PSScriptRoot 'publish'
$publishDir = Join-Path $publishRoot $packageName
$zipPath = Join-Path $PSScriptRoot "$packageName.zip"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "Release consistency check passed for $uiVersion." -ForegroundColor Green
Write-Host "Building tester package for $uiVersion..." -ForegroundColor Cyan

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
