# Local Development Guide

## Backend

```bash
dotnet run --project src/LinkGuardiao.Api
```

The API starts on **http://localhost:5000** (HTTP) by default.

### Configuration

`src/LinkGuardiao.Api/appsettings.Development.json` already includes CORS origins for the frontend dev server:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5174"
    ]
  }
}
```

No AWS credentials are needed for local development — the test suite uses in-memory repositories.
Running the API locally against DynamoDB requires valid AWS credentials and the DynamoDB table names configured (see `.env.example`).

### DynamoDB Local (optional)

If you want to run against a local DynamoDB instead of AWS:

```bash
docker run -p 8000:8000 amazon/dynamodb-local
```

Then set in `appsettings.Development.json`:

```json
{
  "DynamoDb": {
    "ServiceUrl": "http://localhost:8000"
  }
}
```

---

## Frontend

```bash
cd Frontend
npm ci
npm run dev   # starts on http://localhost:5173
```

### Environment variables

Create `Frontend/.env.local` (gitignored):

```
VITE_API_BASE_URL=http://localhost:5000
```

This points the frontend at the local backend. Do **not** commit `.env.local`.

---

## Running Tests

No AWS credentials required — all tests use in-memory repositories.

```bash
# All tests
dotnet test

# Integration tests only
dotnet test tests/LinkGuardiao.Api.Tests

# Unit tests only
dotnet test tests/LinkGuardiao.Application.Tests

# Single test
dotnet test --filter "FullyQualifiedName~LinkFlowTests.CreateLinkAndFetchStats"
```

---

## Frontend Build Check

```bash
cd Frontend && npm run build
```

---

## Port Summary

| Service | Port |
|---|---|
| Backend API | 5000 |
| Frontend dev server | 5173 |
| DynamoDB Local (optional) | 8000 |
