# LinkGuardiao

Encurtador de URLs com autenticacao, senha e estatisticas, pronto para AWS Serverless.

**Acesse a aplicacao: [https://linkguardiao.pages.dev](https://linkguardiao.pages.dev)**

## Stack
- Backend: ASP.NET Core 8, AWS Lambda (Function URL), DynamoDB, JWT, Swagger
- Frontend: React + Vite + TypeScript + Tailwind
- Infra: AWS CDK (TypeScript), GitHub Actions (OIDC)
- Frontend deploy: Cloudflare Pages

## Arquitetura (AWS Serverless)
- Cloudflare Pages serve o frontend via HTTPS
- AWS Lambda publica API HTTPS via Function URL (sem API Gateway)
- DynamoDB on-demand com TTL para expiracao e logs

## DynamoDB model
- Links table: PK = `shortCode`
- GSI1: `userId` (PK) + `createdAt` (SK) para listar links do usuario
- TTL: `expiresAtEpoch` para expiracao automatica
- Access table: PK = `shortCode`, SK = `accessTime` (TTL para logs)
- Daily limits table: PK = `key` (TTL diario)

## Estrutura do repo
- `src/LinkGuardiao.Api`: controllers, middleware, configuracao
- `src/LinkGuardiao.Application`: DTOs, validacoes, casos de uso
- `src/LinkGuardiao.Infrastructure`: DynamoDB, auth
- `infra/cdk`: IaC AWS (CDK)
- `scripts/aws`: build/deploy/destroy e migracao opcional
- `docs`: guias de deploy e seguranca

## Setup local
```bash
# backend (usa AWS DynamoDB com variaveis locais)
cp .env.example .env
# ajuste AWS_REGION e tabelas

dotnet run --project src/LinkGuardiao.Api
```

Frontend:
```bash
cd Frontend
npm ci
cp .env.example .env
npm run dev
```

## Variaveis principais
Backend:
- `AWS_REGION`
- `DDB_TABLE_LINKS`
- `DDB_TABLE_USERS`
- `DDB_TABLE_ACCESS`
- `DDB_TABLE_DAILY_LIMITS`
- `JWT__SECRET` (>= 32 chars)
- `JWT__ISSUER`
- `JWT__AUDIENCE`
- `JWT__ACCESSTOKENMINUTES`
- `CORS_ALLOWED_ORIGIN`
- `LINKLIMITS__DAILYUSERCREATELIMIT`

Frontend:
- `VITE_API_BASE_URL` (Function URL da Lambda)

## Deploy AWS (CDK)
0) Bootstrap (uma vez por conta/regiao):
```powershell
cd infra\cdk
npx cdk bootstrap
cd ..\..
```
1) Build Lambda:
```powershell
scripts\aws\build-lambda.ps1
```
2) Deploy:
```powershell
scripts\aws\deploy.ps1 -Env dev -JwtSecret "SUA_CHAVE_FORTE" -CorsAllowedOrigin "https://linkguardiao.pages.dev"
```
3) Destroy (rollback):
```powershell
scripts\aws\destroy.ps1 -Env dev
```

## Migracao de dados (opcional)
```bash
export SQLITE_PATH=/caminho/para/linkguardiao.db
export DDB_TABLE_LINKS=linkguardiao-links-dev
export DDB_TABLE_USERS=linkguardiao-users-dev
export DDB_TABLE_ACCESS=linkguardiao-access-dev

python scripts/aws/migrate_sqlite_to_dynamo.py
```

## CI/CD
Workflow em `.github/workflows/deploy.yml` com OIDC.
Secrets necessarios:
- `AWS_ROLE_TO_ASSUME`
- `AWS_REGION`
- `JWT_SECRET`
- `CORS_ALLOWED_ORIGIN`

## Observabilidade
- Logs estruturados via Serilog + requestId
- `/health` para monitoramento

## SECURITY AUDIT REPORT
- [x] Rate limiting global e por endpoint com 429 + Retry-After
- [x] Validacao estrita de URL (apenas http/https)
- [x] CORS estrito para o dominio do Pages
- [x] JWT com issuer/audience e expiracao curta
- [x] Hash de senha PBKDF2
- [x] TTL em DynamoDB para expiracao e logs
- [x] Contador diario por usuario (DynamoDB + TTL)
- [x] Headers de seguranca basicos aplicados
- [x] Logs estruturados com requestId e userId

## ACCEPTANCE TESTS
```bash
# build + tests

dotnet build

dotnet test

# cdk synth

cd infra/cdk
npm ci
npx cdk synth -c env=dev

# exemplo de rate limit (espera 429)
for i in {1..20}; do curl -i https://<FUNCTION_URL>/api/auth/login -H "Content-Type: application/json" -d '{"email":"x@y.com","password":"x"}'; done
```

## Curriculo (PT-BR)
- Migrei um encurtador de URLs para AWS Serverless com .NET 8, DynamoDB e Function URL, mantendo custo no free tier.
- Modelei DynamoDB com GSI para consultas por usuario e TTL para expiracao automatica.
- Implementei hardening (rate limit, CORS estrito, JWT, headers) e logs estruturados com requestId.
- Automatizei deploy e rollback com AWS CDK e GitHub Actions (OIDC).
