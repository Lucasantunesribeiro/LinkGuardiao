$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."

# Build API Lambda
$publishDir = Join-Path $root "artifacts\lambda"
if (Test-Path $publishDir) {
  Remove-Item -Recurse -Force $publishDir
}
$projectPath = Join-Path $root "src\LinkGuardiao.Api\LinkGuardiao.Api.csproj"
dotnet publish $projectPath -c Release -r linux-x64 --self-contained false -o $publishDir /p:GenerateRuntimeConfigurationFiles=true

# Build Analytics Consumer Lambda
$consumerPublishDir = Join-Path $root "artifacts\consumer"
if (Test-Path $consumerPublishDir) {
  Remove-Item -Recurse -Force $consumerPublishDir
}
$consumerProjectPath = Join-Path $root "src\LinkGuardiao.AnalyticsConsumer\LinkGuardiao.AnalyticsConsumer.csproj"
if (Test-Path $consumerProjectPath) {
  dotnet publish $consumerProjectPath -c Release -r linux-x64 --self-contained false -o $consumerPublishDir /p:GenerateRuntimeConfigurationFiles=true
  Write-Host "Consumer Lambda built to $consumerPublishDir"
} else {
  Write-Host "Consumer project not found, skipping consumer build."
}
