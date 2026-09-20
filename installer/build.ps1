<#
  Génère la distribution de V0X Cleaner.
  Usage : powershell -File installer\build.ps1 [-FrameworkDependent]
  Sortie : artifacts\publish (fichiers), artifacts\V0XCleaner-<version>-portable.zip,
           et artifacts\V0XCleaner-Setup-<version>.exe si Inno Setup 6 (iscc.exe) est installé.
#>
param([switch]$FrameworkDependent)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'artifacts'
$publish = Join-Path $out 'publish'
$project = Join-Path $root 'src\V0XCleaner.App\V0XCleaner.App.csproj'

$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
dotnet publish $project -c Release -r win-x64 --self-contained $selfContained -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish a échoué.' }

$zip = Join-Path $out "V0XCleaner-$version-portable.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip
Write-Host "Archive portable : $zip"

$cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
$iscc = if ($cmd) { $cmd.Source } else { $null }
if (-not $iscc) {
    $candidate = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (Test-Path $candidate) { $iscc = $candidate }
}

if ($iscc) {
    & $iscc "/DAppVersion=$version" "/DPublishDir=$publish" "/DOutputDir=$out" (Join-Path $PSScriptRoot 'V0XCleaner.iss')
    if ($LASTEXITCODE -ne 0) { throw 'La compilation Inno Setup a échoué.' }
} else {
    Write-Warning 'Inno Setup 6 introuvable : installeur non généré (https://jrsoftware.org/isinfo.php). Archive portable seule.'
}
