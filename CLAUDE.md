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
| RefreshTokens | `tokenHash` | — | GSI1: `userId` |
| EmailLocks | `email` | — | — |

TTL attribute `expiresAtEpoch` on Links, Access, DailyLimits, and RefreshTokens tables.

Email uniqueness is enforced atomically via a DynamoDB `TransactWriteItems` that writes to both `Users` and `EmailLocks` tables with `attribute_not_exists` conditions. The `DynamoDbUserRepository.CreateAsync` throws `UserExistsException` on a `TransactionCanceledException` with `ConditionalCheckFailed`. `EmailLocksTableName` is optional — if empty, only the user PK condition is enforced.

### Configuration

Backend configuration is read from env vars first, then `appsettings.json`. Key env vars:
- `DDB_TABLE_LINKS`, `DDB_TABLE_USERS`, `DDB_TABLE_ACCESS`, `DDB_TABLE_DAILY_LIMITS`
- `DDB_TABLE_REFRESH_TOKENS`, `DDB_TABLE_EMAIL_LOCKS`
- `SQS_ANALYTICS_QUEUE_URL` — if set, redirect click events are enqueued to SQS; otherwise `NoOpAnalyticsQueue` is used
- `STORAGE_PROVIDER` — `dynamodb` (default) or `postgresql` (uses EF Core + Npgsql instead)
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

## MCPs Available in Claude Code

When running Claude Code with MCP servers configured, use these for their respective functions:

| MCP | When to use |
|---|---|
| `awslabs-api` | Call AWS CLI commands (describe Lambda, DynamoDB scan, SQS messages) |
| `awslabs-cfn` | Read or update CloudFormation/CDK stack resource state |
| `awslabs-iam` | Create/update IAM roles, policies, and permissions |
| `awslabs-dynamodb` | DynamoDB table design, GSI modeling, capacity planning |
| `awslabs-lambda` | Invoke Lambda functions, check logs |
| `awslabs-docs` | Search official AWS documentation |
| `context7` | Resolve up-to-date docs for any library (EF Core, AWS SDK, etc.) |
| `Ref` | Read external URLs and search documentation for third-party APIs |
| `playwright` | Browser E2E automation, test UI flows |
| `chrome-devtools` | Browser DevTools automation, performance profiling |
| `supabase` | If switching to Supabase PostgreSQL: migrations, table management |
| `shadcn-ui` | Get shadcn/ui component source for the frontend |
| `firecrawl` | Scrape web pages or search the web for research |
| `exa` | Code-specific web search |

## Specialized Agents (use via Agent tool)

| Agent | When to use |
|---|---|
| `backend-architect` | Implement or review backend features, API endpoints, services, repositories |
| `lucas-frontend-engineer` | Build or review React/TypeScript frontend features, pages, components |
| `postgres-architect` | Design PostgreSQL schemas, write EF Core migrations, optimize queries |
| `qa-engineer` | Plan test coverage, write test cases, create pre-PR checklists |
| `sre-observability` | Add monitoring, OpenTelemetry, CloudWatch metrics, alarms |
| `devops-deploy-architect` | Design CI/CD pipelines, CDK stacks, Docker, deployment infra |
| `security-hardening-validator` | Review auth flows, JWT, CORS, API security before deploying |
| `architecture-advisor` | Evaluate architectural decisions, refactoring approaches |
| `code-quality-reviewer` | Review code quality and production-readiness after significant changes |
| `dx-docs-writer` | Write or update README, setup guides, environment docs |
| `Explore` | Explore codebase to understand patterns before planning |
| `Plan` | Design implementation strategy for complex multi-file changes |
