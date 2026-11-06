# MergeCat

Backend for an idle merge game built with ASP.NET Core, PostgreSQL, and Celo blockchain integration. Players sign in with an Ethereum-compatible wallet, buy and merge units on a 12-cell board, earn passive income, and progress through leagues. On-chain purchases unlock timed income boosts and offline reward collection.

This repository contains the API, database migrations, purchase indexer, and deployment configuration. The frontend and smart contract source are maintained outside this repository.

## Features

- **Wallet authentication:** nonce-based message signing, address recovery, and JWT sessions stored in HTTP-only cookies.
- **Board management:** purchase unlocked unit levels, merge equal-level units, move units into empty cells, and remove units.
- **Idle economy:** server-side income calculation, a four-hour standard offline earnings cap, and paid collection of earnings beyond that cap.
- **Progression:** six leagues based on total earnings, with paginated leaderboards and the current player's rank.
- **On-chain purchases:** a background worker reads `Purchased` events from a Celo contract and applies double-income boosts lasting 75 minutes, 4 hours, or 24 hours, or collects offline rewards.
- **Purchase notifications:** authenticated Server-Sent Events (SSE) deliver purchase updates to connected clients after database changes are saved.

## Technology

| Area | Implementation |
| --- | --- |
| API | C#, .NET 10, ASP.NET Core controllers |
| Persistence | PostgreSQL, Entity Framework Core 10, Npgsql |
| Authentication | Ethereum signed messages, JWT bearer authentication and cookies |
| Blockchain | Nethereum, Celo JSON-RPC |
| Streaming | Server-Sent Events, `System.Threading.Channels` |
| Deployment | Multi-stage Docker build, Docker Compose, GitHub Actions on a self-hosted runner |

## How it works

### Authentication and gameplay

The client requests a nonce and signs a message containing it with the player's wallet. The API recovers the signing address, creates a player and board on first login, and issues a session cookie. Nonces expire after ten minutes; sessions expire after thirty days.

Game state lives in PostgreSQL rather than on-chain. Board actions validate ownership through the authenticated player ID. Earnings are calculated from elapsed time when a player collects income or performs a board action, so the backend does not need a timer for every player.

Unit income and purchase cost use configurable growth rates:

```text
income = IncomeBaseRate × IncomeGrowthRate^(level − 1)
cost   = UnitBaseCost × LevelGrowthRate^(level − 1) × DailyPurchaseGrowthRate^purchaseCount
```

Player records use PostgreSQL's `xmin` column for optimistic concurrency control. The unit-purchase path includes retries for concurrency conflicts. Merge operations also create a history record.

### Purchase processing

```text
Celo contract → JSON-RPC → purchase indexer → PostgreSQL → SSE → client
```

The indexer polls every five seconds and waits for the configured confirmation depth before reading events. It processes bounded block ranges and persists its last processed block so it can resume after a restart.

Purchases are identified by transaction hash and log index, with a database uniqueness constraint to prevent duplicate records. The worker applies rewards, records purchases, and advances the checkpoint before publishing notifications. Purchases for wallets without an existing player are logged and skipped, so a player must sign in before purchasing.

The indexer reads public chain data; the backend does not require a wallet private key to process purchases.

## Running locally

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL, either installed locally or running in Docker
- Access to the configured Celo JSON-RPC endpoint
- An Ethereum-compatible wallet for authenticated gameplay

### 1. Clone the repository

```sh
git clone https://github.com/DmitryDatsko/MergeCat.git
cd MergeCat
```

### 2. Start a local database

For example, with Docker:

```sh
docker run --name mergecat-postgres \
  -e POSTGRES_DB=MergeCat \
  -e POSTGRES_PASSWORD=local-development-only \
  -p 127.0.0.1:5433:5432 \
  -v mergecat-local-db:/var/lib/postgresql/data \
  -d postgres:17
```

Wait for PostgreSQL to accept connections before starting the API. An existing PostgreSQL installation works too; create a database and adjust the connection string below.

### 3. Set local configuration

The following commands use a POSIX shell. They override the checked-in connection string and JWT signing secret without editing tracked files:

```sh
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5433;Database=MergeCat;Username=postgres;Password=local-development-only'
export JwtOptions__TokenSecret="$(openssl rand -hex 64)"
```

Do not run against the database address in the checked-in configuration. Use your own database and signing secret. Keep the same secret between restarts if existing local sessions should remain valid.

The default blockchain configuration points to Celo and starts indexing from the checkpoint seeded by the initial migration. On a fresh database, catching up with the chain may take time. The worker starts automatically; there is no configuration switch to disable it.

### 4. Start the API

```sh
dotnet dev-certs https --trust
dotnet restore
dotnet run --launch-profile https
```

The HTTPS endpoint is `https://localhost:7166`. Database migrations are applied automatically at startup, so the configured database user must have permission to create and alter the schema.

Check the unauthenticated nonce endpoint:

```sh
curl https://localhost:7166/api/Auth/nonce
```

In Development, the OpenAPI document is available at `/openapi/v1.json`. A Swagger UI is not included.

Use HTTPS for cookie authentication. Browser clients must send credentials with requests; the development CORS policy includes `http://localhost:3000`. Origins are configured in `Program.cs`, and cross-site deployments also need compatible cookie settings.

## API overview

All routes below require authentication except `/api/Auth/nonce` and `/api/Auth/verify`. JSON request fields use camel case.

| Method | Route | Purpose / request |
| --- | --- | --- |
| GET | `/api/Auth/nonce` | Obtain a nonce in the response's `message` field |
| POST | `/api/Auth/verify` | Authenticate with `{ "message": "...", "signature": "0x..." }` |
| GET | `/api/Auth/me` | Get the authenticated wallet address |
| POST | `/api/Auth/logout` | Clear the session cookie |
| GET | `/api/Board/get-board` | Collect accrued earnings and return board state |
| GET | `/api/Board/get-prices` | Get prices and income rates for unlocked unit levels |
| POST | `/api/Board/buy-unit` | Buy a unit with `{ "level": 1 }` |
| POST | `/api/Board/merge` | Merge units with `{ "fromIndex": 0, "toIndex": 1 }` |
| POST | `/api/Board/move` | Move a unit with `{ "fromIndex": 0, "toIndex": 1 }` |
| POST | `/api/Board/remove-unit` | Remove a unit; body is a JSON integer such as `0` |
| GET | `/api/Player/profile` | Get balance, earnings preview, league, and boost status |
| POST | `/api/Player/collect` | Collect standard earnings |
| GET | `/api/Player/leaderboard?league=Bronze&page=1&pageSize=20` | Get a league leaderboard; page size is capped at 100 |
| GET | `/api/notifications/stream` | Subscribe to SSE events named `purchase` |

To authenticate, request a nonce, include it in a wallet-signed message as `Nonce: <nonce>`, then submit the original message and its signature to `/api/Auth/verify`. Preserve the returned cookie for subsequent requests. Authentication uses Ethereum signed-message recovery; it does not perform full Sign-In with Ethereum message validation.

## Configuration

Settings are defined in `appsettings.json` and bound to classes in `Options/`. ASP.NET Core environment variable overrides use double underscores:

| Section | Settings | Example override |
| --- | --- | --- |
| `ConnectionStrings` | PostgreSQL connection | `ConnectionStrings__DefaultConnection` |
| `JwtOptions` | Signing secret, cookie name, issuer, audience | `JwtOptions__TokenSecret` |
| `GameOptions` | Starting balance, board size, unit costs, income rates, bonus threshold | `GameOptions__StartingBalance` |
| `BlockchainOptions` | RPC URL, contract address, confirmation depth, query range, creation block | `BlockchainOptions__RpcUrl` |

The default board size is 12. Merge, move, and remove validation currently assumes indices `0–11`, so changing `BoardSize` alone is not supported.

The initial migration seeds a fixed indexer checkpoint. Changing `ContractCreationBlock` alone does not reset an existing database's checkpoint; switching contracts or chains also requires reviewing the migration and persisted indexer state.

## Project structure

```text
Controllers/       Authentication, board, and player HTTP endpoints
Endpoints/         SSE notification endpoint
Services/          Earnings, league progression, purchase indexing, notifications
Models/            Database entities, API DTOs, and contract event mapping
Context/           EF Core mappings, indexes, and concurrency configuration
Migrations/        PostgreSQL schema and initial indexer checkpoint
Options/           Typed JWT, game, and blockchain configuration
Program.cs         Dependency registration, middleware, and startup migrations
.github/workflows/ Self-hosted deployment workflow
```

## Deployment and current constraints

The Dockerfile builds and publishes the .NET application in separate stages and runs it under the base image's application user. The GitHub Actions workflow rebuilds and restarts the backend on a self-hosted runner after pushes to `main`.

The checked-in `compose.yaml` describes a larger deployment, not a standalone quick start. It expects a frontend at `../frontend`, an existing external Docker network named `proxy`, and deployment-specific environment variables. Its `EnvVariables__*` mappings do not match the current `JwtOptions` and `GameOptions` sections and must be updated before relying on those overrides.

Before a public deployment:

- Replace the committed database credentials and JWT signing secret. If they have been used, rotate them; removing them from the latest commit does not remove them from Git history.
- Review HTTPS termination, allowed origins, and cookie policy for the actual frontend and API domains.
- Keep in mind that nonce storage and SSE subscriptions are in-memory and process-local. Multiple API instances would require coordination, and SSE notifications have no replay mechanism.
- Treat the confirmation delay as a safeguard, not chain-reorganization recovery: the indexer does not roll back previously processed events.

There is currently no automated test project in this repository. The deployment workflow checks container status but does not run tests or an application-level health probe.
