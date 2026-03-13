# LinkGuardiao

Encurtador de URLs com autenticacao, links protegidos por senha, expiracao, analytics assincrono e stack AWS serverless.

**Acesse a aplicacao: [https://linkguardiao.pages.dev](https://linkguardiao.pages.dev)**

## Stack
- Backend: ASP.NET Core 8, AWS Lambda behind API Gateway HTTP API, DynamoDB, PostgreSQL (opcional), Redis (opcional), JWT, Swagger
- Frontend: React + Vite + TypeScript + Tailwind
- Mensageria: Amazon SQS + Lambda consumer com retry, DLQ e deduplicacao de eventos
- Infra: AWS CDK (TypeScript), GitHub Actions (OIDC), Docker Compose para desenvolvimento local
- Frontend deploy: Cloudflare Pages

## Arquitetura (AWS Serverless)
- Cloudflare Pages serve o frontend via HTTPS
- AWS API Gateway HTTP API publica a API ASP.NET Core hospedada em AWS Lambda
- DynamoDB on-demand com TTL para expiracao, logs, refresh tokens e lock de e-mail
- SQS desacopla o redirect do processamento de analytics
- Redis pode ser habilitado para cache distribuido de leitura
- Existe trilha alternativa em PostgreSQL com EF Core e migrations para demonstrar persistencia relacional

## DynamoDB model
- Links table: PK = `shortCode`
- GSI1: `userId` (PK) + `createdAt` (SK) para listar links do usuario
- Access table: PK = `shortCode`, SK = `accessTime` (TTL para logs)
- Daily limits table: PK = `key` (TTL diario)
- Refresh tokens table: PK = `tokenHash`, GSI `userId`
- Email locks table: PK = `email`
- TTL principal: `expiresAtEpoch`

## Estrutura do repo
- `src/LinkGuardiao.Api`: controllers, middleware, health checks e composicao da aplicacao
- `src/LinkGuardiao.Application`: DTOs, contratos, validacoes, servicos e telemetria
- `src/LinkGuardiao.Infrastructure`: DynamoDB, SQS, auth, cache e seguranca
- `src/LinkGuardiao.Infrastructure.PostgreSQL`: EF Core, migrations e repositories PostgreSQL
- `src/LinkGuardiao.AnalyticsConsumer`: consumer SQS para analytics
- `Frontend`: aplicacao React + Vite
- `infra/cdk`: infraestrutura AWS como codigo
- `docs`: guias de deploy e operacao

## Setup local
Backend:
```bash
cp .env.example .env
docker compose up -d
pwsh ./scripts/local/init-dynamodb.ps1
dotnet ef database update --project src/LinkGuardiao.Infrastructure.PostgreSQL --startup-project src/LinkGuardiao.Infrastructure.PostgreSQL
dotnet run --project src/LinkGuardiao.Api
```

Frontend:
```bash
cd Frontend
npm ci
cp .env.example .env
npm run dev
```

Resumo do ambiente local:
- API: `http://localhost:5000`
- PostgreSQL: `localhost:54329`
- DynamoDB Local: `http://localhost:8000`
- Redis: `localhost:6380`

Guia completo: `docs/local-dev.md`.

## Variaveis principais
Backend:
- `AWS_REGION`
- `DDB_TABLE_LINKS`
- `DDB_TABLE_USERS`
- `DDB_TABLE_ACCESS`
- `DDB_TABLE_DAILY_LIMITS`
- `DDB_TABLE_REFRESH_TOKENS`
- `DDB_TABLE_EMAIL_LOCKS`
- `DDB_SERVICE_URL`
- `STORAGE_PROVIDER`
- `POSTGRESQL_CONNECTION_STRING`
- `REDIS_CONNECTION_STRING`
- `JWT__SECRET` (>= 32 chars)
- `JWT__ISSUER`
- `JWT__AUDIENCE`
- `JWT__ACCESSTOKENMINUTES`
- `CORS_ALLOWED_ORIGIN`
- `LINKLIMITS__DAILYUSERCREATELIMIT`

Frontend:
- `VITE_API_BASE_URL` (URL base do API Gateway HTTP API, sem `/api`)

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
3) Capture o output `ApiEndpoint` do stack para configurar o frontend:
```text
https://<api-id>.execute-api.<region>.amazonaws.com
```
4) Destroy (rollback):
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
`ci.yml` executa build Release, testes .NET com coverage gate, lint/test/build do frontend, `npm audit` no frontend e na stack CDK, `dotnet list package --vulnerable` e `cdk synth`.

`deploy.yml` faz o deploy AWS manual com OIDC.

Secrets necessarios:
- `AWS_ROLE_TO_ASSUME`
- `AWS_REGION`
- `JWT_SECRET`
- `CORS_ALLOWED_ORIGIN`
- `VITE_API_BASE_URL`

## Observabilidade
- Logs estruturados via Serilog + requestId
- `/health`, `/health/live` e `/health/ready`
- Metricas basicas para redirects, deduplicacao de analytics e falhas do consumer

## Security Audit Report
- [x] Rate limiting global e por endpoint com `429` + `Retry-After`
- [x] Validacao estrita de URL (apenas `http`/`https`)
- [x] CORS estrito para o dominio do Pages
- [x] JWT com issuer/audience e expiracao curta
- [x] Hash de senha PBKDF2
- [x] TTL em DynamoDB para expiracao e logs
- [x] Contador diario por usuario
- [x] Headers de seguranca basicos aplicados
- [x] Links protegidos por senha usam access grant temporario e redirect oficial
- [x] Analytics assincrono com deduplicacao no consumer

## Acceptance Tests
```bash
dotnet build LinkGuardiao.sln --configuration Release
dotnet test LinkGuardiao.sln
dotnet list LinkGuardiao.sln package --vulnerable --include-transitive

cd Frontend
npm ci
npm run lint
npm run test:run
npm run build
npm audit --audit-level=high

cd ../infra/cdk
npm ci
npm run build
npx cdk synth -c env=dev
npm audit --audit-level=high
```

## Curriculo (PT-BR)
- Desenvolvi um encurtador de URLs serverless com .NET 8, React, AWS Lambda e API Gateway HTTP API.
- Modelei DynamoDB com GSI e TTL, mantendo uma trilha alternativa em PostgreSQL com EF Core e migrations.
- Implementei links protegidos por senha, refresh token, rate limit, cache opcional com Redis, SQS com DLQ e deduplicacao de analytics.
- Automatizei CI/CD com GitHub Actions, quality gates, auditoria de dependencias e infraestrutura como codigo em AWS CDK.
