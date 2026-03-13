# Local Development Guide

## 1. Start dependencies

Use the shared local stack to run PostgreSQL, DynamoDB Local and Redis:

```bash
docker compose up -d
```

## 2. Environment

Copy the backend env file and keep the defaults if you want to use the local stack:

```bash
cp .env.example .env
```

Important defaults already included in `.env.example`:

- `DDB_SERVICE_URL=http://localhost:8000`
- `POSTGRESQL_CONNECTION_STRING=Host=localhost;Port=54329;Database=linkguardiao;Username=linkguardiao;Password=linkguardiao`
- `REDIS_CONNECTION_STRING=localhost:6380`
- `DDB_TABLE_REFRESH_TOKENS=linkguardiao-refresh-tokens-dev`
- `DDB_TABLE_EMAIL_LOCKS=linkguardiao-email-locks-dev`

To run the API on PostgreSQL instead of DynamoDB, set:

```bash
STORAGE_PROVIDER=postgresql
```

## 3. Bootstrap PostgreSQL schema

Apply the EF Core migration:

```bash
dotnet ef database update --project src/LinkGuardiao.Infrastructure.PostgreSQL --startup-project src/LinkGuardiao.Infrastructure.PostgreSQL
```

## 4. Bootstrap DynamoDB Local tables

Create the DynamoDB Local tables used by links, auth, analytics and rate limits:

```bash
pwsh ./scripts/local/init-dynamodb.ps1
```

## 5. Run the backend

```bash
dotnet run --project src/LinkGuardiao.Api
```

The API starts on `http://localhost:5000`.

## 6. Run the frontend

```bash
cd Frontend
npm ci
npm run dev
```

`Frontend/.env.example` already points to `http://localhost:5000`.

## 7. Validation commands

```bash
dotnet test LinkGuardiao.sln
cd Frontend && npm run lint && npm run test:run && npm run build
cd infra/cdk && npm ci && npm run build && npx cdk synth -c env=dev
```

## Port summary

| Service | Port |
|---|---|
| Backend API | 5000 |
| Frontend dev server | 5173 |
| PostgreSQL | 54329 |
| DynamoDB Local | 8000 |
| Redis | 6380 |
