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

# Resolve AWS account/region for CDK
$env:CDK_DEFAULT_ACCOUNT = (aws sts get-caller-identity --query Account --output text)
$env:CDK_DEFAULT_REGION  = (aws configure get region)
if (-not $env:CDK_DEFAULT_REGION) { $env:CDK_DEFAULT_REGION = "us-east-1" }
Write-Host "Deploying to account $($env:CDK_DEFAULT_ACCOUNT) / $($env:CDK_DEFAULT_REGION)"

& "$PSScriptRoot\build-lambda.ps1"

Push-Location (Join-Path $root "infra\cdk")

npm ci
npx cdk bootstrap "aws://$($env:CDK_DEFAULT_ACCOUNT)/$($env:CDK_DEFAULT_REGION)" --require-approval never
npx cdk deploy "LinkGuardiao-$Env" -c env=$Env -c corsAllowedOrigin=$CorsAllowedOrigin --require-approval never --parameters JwtSecret=$JwtSecret

Pop-Location
