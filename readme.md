# LinkGuardiao

LinkGuardiao e um encurtador de URLs com autenticacao, protecao por senha e estatisticas de acesso.

## Stack
- Backend: ASP.NET Core 8, EF Core, JWT, Swagger
- Frontend: React + Vite + TypeScript + Tailwind + Chart.js
- Infra: Docker, Postgres (prod), SQLite (dev)

## Arquitetura
- `src/LinkGuardiao.Api`: controllers, middleware, configuracao
- `src/LinkGuardiao.Application`: DTOs, validacao, casos de uso
- `src/LinkGuardiao.Infrastructure`: EF Core, auth, background jobs
- `tests/`: unitarios e integracao

## Rodar localmente
### Backend
```bash
dotnet run --project src/LinkGuardiao.Api
```

### Frontend
```bash
cd Frontend
npm ci
cp .env.example .env
npm run dev
```
## Variaveis principais
- Backend: `ConnectionStrings__Default`, `Jwt__Secret` (min 32 chars), `Cors__AllowedOrigins__0`
- Frontend: `VITE_API_BASE_URL`

## Testes
```bash
dotnet test
```

```bash
cd Frontend
npm run build
```

## Docker (dev)
```bash
docker compose -f docker-compose.local.yml up --build
```

## Deploy
- Frontend: `docs/DEPLOY_CLOUDFLARE_PAGES.md`
- Backend: `docs/DEPLOY_ORACLE_ALWAYS_FREE.md`

## CI/CD
- CI: `.github/workflows/ci.yml`
- Deploy manual: `.github/workflows/deploy.yml`
