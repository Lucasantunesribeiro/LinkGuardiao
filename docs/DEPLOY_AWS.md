# Deploy AWS (Serverless)

## Pre-requisitos
- AWS CLI configurado
- Node 20+
- .NET 8 SDK
- Permissoes para criar Lambda, API Gateway, DynamoDB, SQS e WAF

## Validacao obrigatoria de credenciais
```bash
aws sts get-caller-identity
aws configure list
```

Confirme:
- `AWS_PROFILE=default`
- `AWS_REGION=<regiao desejada>`

## Deploy manual (PowerShell)
```powershell
# bootstrap (uma vez por conta/regiao)
cd infra\cdk
npx cdk bootstrap
cd ..\..

# build lambda
scripts\aws\build-lambda.ps1

# deploy
scripts\aws\deploy.ps1 -Env dev -JwtSecret "SUA_CHAVE_FORTE" -CorsAllowedOrigin "https://linkguardiao.pages.dev"
```

## Rollback / teardown
```powershell
scripts\aws\destroy.ps1 -Env dev
```

## Endpoint da API
O stack CDK exporta o output `ApiEndpoint`, que representa a URL publica do API Gateway HTTP API.

Exemplo:
```text
https://<api-id>.execute-api.<region>.amazonaws.com
```

## Testes rapidos
```bash
# health
curl -i https://<API_ENDPOINT>/health

# register
curl -i https://<API_ENDPOINT>/api/auth/register -H "Content-Type: application/json" -d '{"name":"User","email":"user@example.com","password":"Secret123"}'

# create link (use o token JWT)
curl -i https://<API_ENDPOINT>/api/links -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json" -d '{"originalUrl":"https://example.com"}'

# redirect
curl -i https://<API_ENDPOINT>/r/<SHORTCODE>
```

## Cloudflare Pages
Defina em `VITE_API_BASE_URL` o output `ApiEndpoint` do stack CDK, sem adicionar `/api`.

## Migracao opcional (SQLite -> DynamoDB)
```bash
export SQLITE_PATH=/caminho/para/linkguardiao.db
export DDB_TABLE_LINKS=linkguardiao-links-dev
export DDB_TABLE_USERS=linkguardiao-users-dev
export DDB_TABLE_ACCESS=linkguardiao-access-dev

python scripts/aws/migrate_sqlite_to_dynamo.py
```

## CI/CD (OIDC)
Secrets esperados no GitHub:
- `AWS_ROLE_TO_ASSUME`
- `AWS_REGION`
- `JWT_SECRET`
- `CORS_ALLOWED_ORIGIN`
- `VITE_API_BASE_URL`

Execute o workflow `Deploy AWS (Manual)`.
