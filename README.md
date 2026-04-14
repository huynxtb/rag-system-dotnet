# RAG System Demo

A role-based document Retrieval-Augmented-Generation playground built with **.NET 10**, **React + TypeScript + i18n**, **MongoDB**, **Qdrant**, **OpenAI**, and **JWT** — laid out with **Clean Architecture + DDD**.

It implements the spec in `DOCX.md`:

- Login (no public registration)
- Per-document role-based read access (rejects with the required role list when denied)
- Upload with mandatory **type** + **allowed roles**, including 3 built-in types: *tài chính*, *chính sách bảo mật*, *khác*
- Create new custom types
- Re-uploading a document of the same name & type **re-chunks, re-embeds, and bumps to a new version**
- Chat endpoint that retrieves only chunks from documents the caller is allowed to read

## Architecture

```
RagSystem.sln
├── Directory.Build.props          ← target framework + central PM
├── Directory.Packages.props       ← central package versions
├── docker-compose.yml             ← MongoDB + Qdrant
├── src/
│   ├── RagSystem.Domain/          ← entities (User, Document, DocumentType), invariants
│   ├── RagSystem.Application/     ← use cases, DTOs, port interfaces (Clean Arch boundary)
│   ├── RagSystem.Infrastructure/  ← Mongo, Qdrant, OpenAI, JWT, BCrypt, file extractors, seeder
│   └── RagSystem.Api/             ← ASP.NET Core controllers, DI, JWT auth, Swagger, CORS
└── client/                        ← React + Vite + TypeScript + i18next (EN / VI)
```

### Domain rules

- **User**: `Id`, `Email`, `PasswordHash`, `Roles[]` — multiple roles allowed.
- **Document**: `Id`, `FileName`, `Type`, `Version`, `UploadedAt`, `Status`, `AllowedRoles[]`, `UploadedBy`, `SizeBytes`, `ContentHash`.
  - `CanBeReadBy(userRoles)` is the single guard for read access.
  - `Version` is monotonic per `(FileName, Type)`.
- **DocumentType**: name + display name; `IsBuiltIn` distinguishes seeded vs. user-created.

### Vector store

Qdrant collection `rag_documents` (cosine, 1536-dim by default to match `text-embedding-3-small`). Each chunk is stored with payload `{document_id, document_type, version, chunk_index, text, allowed_roles}`. Search is filtered server-side: at least one of the caller's roles must overlap with `allowed_roles`, so unauthorized chunks never reach the LLM.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for MongoDB + Qdrant)
- An [OpenAI API key](https://platform.openai.com/api-keys)

## Run it

You can run the whole stack in Docker **or** run the API / client locally against dockerised Mongo + Qdrant.

### Option A — Everything in Docker (recommended)

```bash
cp .env.example .env          # then edit .env to add your OPENAI_API_KEY
docker compose up -d --build
```

Services that come up:

| Service  | Container    | Host URL                       |
| -------- | ------------ | ------------------------------ |
| Mongo    | `rag-mongo`  | `localhost:27017`              |
| Qdrant   | `rag-qdrant` | `localhost:6333` / `:6334`     |
| API      | `rag-api`    | http://localhost:5080          |
| Client   | `rag-client` | http://localhost:5173          |

The client container runs nginx and proxies `/api/*` to the API container over the compose network. API picks up config from env vars (`Mongo__*`, `Qdrant__*`, `OpenAi__*`, `Jwt__*`).

Tail logs:

```bash
docker compose logs -f api
```

Tear down (keeps volumes):

```bash
docker compose down
```

Wipe data too:

```bash
docker compose down -v
```

### Option B — Local dev (hot reload)

Start just the infra:

```bash
docker compose up -d mongo qdrant
```

Store your OpenAI key in **.NET user secrets** (not in `appsettings.json`):

```bash
dotnet user-secrets set "OpenAi:ApiKey" "sk-..." --project src/RagSystem.Api
```

Then run:

```bash
dotnet run --project src/RagSystem.Api
```

API listens on **http://localhost:5080**, OpenAPI at **/openapi/v1.json**, UI at **/scalar/v1**.
On first start the seeder creates the two test accounts and the three built-in document types.

React client:

```bash
cd client
npm install
npm run dev
```

The Vite dev server runs on **http://localhost:5173** and proxies `/api/*` to the API.

## Test accounts

| Email             | Password    | Roles          |
| ----------------- | ----------- | -------------- |
| `admin@gmail.com` | `12345678Aa` | `admin`, `user` |
| `user@gmail.com`  | `12345678Aa` | `user`         |

Try the role-based access flow:

1. Sign in as `admin@gmail.com`, upload a document with allowed roles = `admin` only.
2. Sign out, sign in as `user@gmail.com` — the document will not appear in the list, and asking about it via chat returns no context.
3. Re-upload the same file with the same type as `admin@gmail.com` — version is bumped to **v2**, the old chunks remain in Qdrant under v1, new chunks land under v2.

## API surface

| Method | Path                    | Auth   | Notes                                                |
| ------ | ----------------------- | ------ | ---------------------------------------------------- |
| POST   | `/api/auth/login`       | No     | `{ email, password }` → `{ token, email, roles }`     |
| GET    | `/api/me`               | JWT    | Echo current claims                                  |
| GET    | `/api/document-types`   | JWT    | List built-in + custom types                         |
| POST   | `/api/document-types`   | JWT    | `{ name, displayName }` create new type              |
| GET    | `/api/documents`        | JWT    | List documents the caller's roles can read           |
| GET    | `/api/documents/{id}`   | JWT    | 403 with `requiredRoles` if caller lacks access      |
| POST   | `/api/documents/upload` | JWT    | `multipart`: `file`, `type`, `allowedRoles` (csv)    |
| POST   | `/api/chat/ask`         | JWT    | `{ question, topK }` → answer + cited sources         |

Supported upload formats: `.pdf`, `.docx`, `.txt`, `.md`.

## Tech stack

- **Backend** — .NET 10 (`net10.0`), ASP.NET Core, MongoDB.Driver, Qdrant.Client (gRPC), OpenAI .NET SDK, BCrypt, JWT bearer, central package management via `Directory.Packages.props`
- **Frontend** — React 18, TypeScript, Vite, react-router, i18next + browser language detector (English & Vietnamese)
- **Infrastructure** — MongoDB 7, Qdrant v1.12, Docker Compose

## Troubleshooting

- **`MongoConnectionException`** — confirm `docker compose ps` shows `rag-mongo` is up; the API will keep retrying on subsequent calls.
- **`The model produced invalid content` / 401 from OpenAI** — set a valid `OpenAi:ApiKey`. You can also set the embedding/chat model names in `appsettings.json`.
- **`Vector dimension mismatch` from Qdrant** — drop the `rag_documents` collection in Qdrant if you switch embedding models with different dimensions.
- **CORS errors in the browser** — the API allows `http://localhost:5173` and `http://localhost:3000` by default; add your origin in `Program.cs`.
