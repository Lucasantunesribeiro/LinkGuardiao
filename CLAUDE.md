# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend
```bash
dotnet run --project src/LinkGuardiao.Api
dotnet build
dotnet test                                          # all tests
dotnet test tests/LinkGuardiao.Api.Tests             # integration tests only
dotnet test tests/LinkGuardiao.Application.Tests     # unit tests only
dotnet test --filter "FullyQualifiedName~LinkFlowTests.CreateLink"  # single test
```

### Frontend
```bash
cd Frontend
npm ci
npm run dev        # dev server (http://localhost:5173)
npm run build
npm run lint
```

### AWS CDK (infra)
```bash
cd infra/cdk
npm ci
npx cdk synth -c env=dev
npx cdk bootstrap    # once per account/region
```

### AWS Deploy
```powershell
scripts\aws\build-lambda.ps1
scripts\aws\deploy.ps1 -Env dev -JwtSecret "..." -CorsAllowedOrigin "https://..."
scripts\aws\destroy.ps1 -Env dev
```

## Architecture

**LinkGuardiao** is a URL shortener with authentication, password protection, and click statistics deployed as AWS Serverless (Lambda + DynamoDB).

### Backend (Clean Architecture, 3 layers)

- **`src/LinkGuardiao.Api`** – HTTP layer. `Program.cs` bootstraps Serilog and delegates to `Startup.cs`. `Startup.cs` registers all services and configures the middleware pipeline (RequestId → Serilog → ExceptionHandling → SecurityHeaders → CORS → RateLimiter → Auth). `LambdaEntryPoint.cs` wraps the same `Startup` for Lambda via `APIGatewayHttpApiV2ProxyFunction`.

- **`src/LinkGuardiao.Application`** – Business logic. Contains entities, DTOs, service interfaces (`ILinkRepository`, `IUserRepository`, `IAccessLogRepository`, `IDailyLimitStore`), service implementations (`LinkService`, `UserService`, `StatsService`), FluentValidation validators, and options classes. Has no infrastructure dependencies.

- **`src/LinkGuardiao.Infrastructure`** – Infrastructure implementations. `Data/DynamoDb*.cs` implement the repository interfaces against DynamoDB. `Security/` holds `JwtTokenService` and `Pbkdf2PasswordHasher`.

### DynamoDB Schema

| Table | PK | SK | GSI |
|---|---|---|---|
| Links | `shortCode` | — | GSI1: `userId` + `createdAt` |
| Users | `userId` | — | GSI1: `email` |
| Access | `shortCode` | `accessTime` | — |
| DailyLimits | `key` | — | — |

TTL attribute `expiresAtEpoch` on Links, Access, and DailyLimits tables.

### Configuration

Backend configuration is read from env vars first, then `appsettings.json`. Key env vars:
- `DDB_TABLE_LINKS`, `DDB_TABLE_USERS`, `DDB_TABLE_ACCESS`, `DDB_TABLE_DAILY_LIMITS`
- `JWT__SECRET` (≥32 chars), `JWT__ISSUER`, `JWT__AUDIENCE`, `JWT__ACCESSTOKENMINUTES` (5–120)
- `CORS_ALLOWED_ORIGIN` (single origin) or `Cors:AllowedOrigins` array in config
- `LINKLIMITS__DAILYUSERCREATELIMIT`

### Testing Strategy

Tests use `WebApplicationFactory<Program>` (`ApiTestFactory`) which replaces all DynamoDB services with in-memory implementations (`InMemoryLinkRepository`, `InMemoryUserRepository`, etc.) from `tests/LinkGuardiao.Api.Tests/InMemoryRepositories.cs`. No real AWS credentials are needed to run tests.

Unit tests in `LinkGuardiao.Application.Tests` instantiate services directly with local in-memory fakes.

### Frontend

React + Vite + TypeScript + Tailwind. API calls go through `Frontend/src/lib/api/client.ts` (axios, with a request interceptor that injects the JWT from `localStorage` and a response interceptor that redirects to `/login` on 401). Auth state lives in `Frontend/src/features/auth/AuthContext.tsx`. `VITE_API_BASE_URL` must point to the Lambda Function URL.

Frontend is deployed to **Cloudflare Pages** (`linkguardiao` project) automatically by CI on every push to `main`. Required GitHub secrets: `CLOUDFLARE_API_TOKEN`, `CLOUDFLARE_ACCOUNT_ID`, `VITE_API_BASE_URL`.

Short-code redirect routes are handled client-side: `/:shortCode` and `/r/:shortCode` both render `RedirectPage`, which calls the backend to resolve and redirect.

### Rate Limiting

Enforced in `Startup.cs` using ASP.NET Core's built-in rate limiter:
- Global: 200 req/min per client
- `auth` policy: 10 req/min (login/register)
- `link-create` policy: 10 req/min
- `redirect` policy: 120 req/min
- `stats` policy: 60 req/min

Client identity is user ID (if authenticated) or IP address.
