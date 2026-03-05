param(
  [string]$Env = "dev"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\.."

Push-Location (Join-Path $root "infra\cdk")

npm ci
npx cdk destroy "LinkGuardiao-$Env" -c env=$Env --force

Pop-Location
