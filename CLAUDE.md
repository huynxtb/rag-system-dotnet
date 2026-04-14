# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

### Backend (.NET 10)

```bash
# Build the solution
dotnet build

# Run the API (hot reload)
dotnet run --project src/RagSystem.Api

# Set OpenAI key via user secrets (required before first run)
dotnet user-secrets set "OpenAi:ApiKey" "sk-..." --project src/RagSystem.Api
```

### Frontend (React + Vite)

```bash
cd client
npm install
npm run dev          # dev server on http://localhost:5173
npm run build        # tsc -b && vite build
```

### Infrastructure (Docker Compose)

```bash
docker compose up -d mongo qdrant    # local dev: just databases
docker compose up -d --build         # full stack (needs .env with OPENAI_API_KEY)
docker compose down -v               # tear down + wipe data
```

### No test suite exists in this repository.

## Architecture

Clean Architecture + DDD with four layers. Dependencies flow inward: Api -> Infrastructure -> Application -> Domain.

- **RagSystem.Domain** — Pure domain entities (`User`, `Document`, `DocumentType`), value objects, enums (`DocumentStatus`, `Role`), and invariants. No external dependencies.
- **RagSystem.Application** — Use-case services (`AuthService`, `DocumentService`, `DocumentTypeService`), DTOs, and port interfaces (abstractions like `IVectorStore`, `IEmbeddingService`, `IDocumentRepository`). Depends only on Domain.
- **RagSystem.Infrastructure** — Adapter implementations: MongoDB repositories, Qdrant vector store (gRPC), OpenAI embedding/chat, BCrypt password hashing, JWT token generation, PDF/DOCX/text extractors, data seeder. Depends on Application.
- **RagSystem.Api** — ASP.NET Core host: controllers, JWT auth middleware, `ExceptionHandlingMiddleware`, DI wiring, CORS, Scalar OpenAPI docs. Depends on Infrastructure.

### Key architectural patterns

- **Port/adapter via DI**: Application layer defines interfaces (e.g. `IVectorStore`, `IEmbeddingService`, `IChatCompletionService`); Infrastructure implements them. All wired in `Program.cs`.
- **`ICurrentUser` scoped service**: Extracts user identity from `HttpContext` claims. Injected into application services for role-based filtering.
- **Central package management**: Package versions are pinned in `Directory.Packages.props`; individual csproj files reference packages without versions.
- **`DataSeeder` (IHostedService)**: Runs at startup to create built-in document types and seed test accounts.

### RAG pipeline flow

1. **Upload**: File -> `DocumentTextExtractor` (PDF via PdfPig, DOCX via OpenXml, or plain text) -> `PlainTextChunker` -> `OpenAiEmbeddingService` (batch embed) -> `QdrantVectorStore` (upsert with role metadata).
2. **Query**: User question -> embed -> Qdrant filtered search (only chunks where user's roles overlap `allowed_roles` via `filter.Should`) -> top-K chunks as context -> OpenAI chat completion -> answer + sources.
3. **Re-upload**: Same `(fileName, type)` pair bumps `Version` monotonically. Old chunks remain in Qdrant under previous version; new chunks are stored under the new version.

### Role-based access control

- `Document.CanBeReadBy(userRoles)` is the single authorization guard — checks intersection of user roles with `AllowedRoles`.
- Qdrant search is also server-side filtered by roles, so unauthorized chunks never reach the LLM.
- `ForbiddenAccessException` returns 403 with the list of required roles.

### Frontend

React 18 SPA with Vite, react-router, and i18next (EN/VI). Vite dev server proxies `/api/*` to `http://localhost:5080`. Auth via JWT stored client-side with an `AuthProvider` context. Routes: `/login`, `/documents`, `/upload`, `/types`, `/chat`.

## Configuration

API configuration is via `appsettings.json` or environment variables with sections: `Mongo`, `Qdrant`, `OpenAi`, `Jwt`. The `OpenAi:ApiKey` should be stored in .NET user secrets for local dev, or in a `.env` file for Docker Compose.

Default Qdrant collection: `rag_documents` (cosine distance, 1536-dim for `text-embedding-3-small`). Changing embedding models requires dropping and recreating the collection.

## Seeded test accounts

| Email             | Password     | Roles          |
|-------------------|-------------|----------------|
| admin@gmail.com   | 12345678Aa  | admin, user    |
| user@gmail.com    | 12345678Aa  | user           |
