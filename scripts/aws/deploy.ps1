param(
  [string]$Env = "dev",
  [string]$JwtSecret,
  [string]$CorsAllowedOrigin = "https://linkguardiao.pages.dev"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
  throw "JwtSecret is required."
}

$root = Resolve-Path "$PSScriptRoot\..\.."

& "$PSScriptRoot\build-lambda.ps1"

Push-Location (Join-Path $root "infra\cdk")

npm ci
npx cdk deploy "LinkGuardiao-$Env" -c env=$Env -c corsAllowedOrigin=$CorsAllowedOrigin --require-approval never --parameters JwtSecret=$JwtSecret

Pop-Location
