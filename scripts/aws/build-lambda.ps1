$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$publishDir = Join-Path $root "artifacts\lambda"

if (Test-Path $publishDir) {
  Remove-Item -Recurse -Force $publishDir
}

$projectPath = Join-Path $root "src\LinkGuardiao.Api\LinkGuardiao.Api.csproj"

dotnet publish $projectPath -c Release -r linux-x64 --self-contained false -o $publishDir /p:GenerateRuntimeConfigurationFiles=true
