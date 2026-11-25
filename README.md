# Dev environment: SQL Server + FastEndpoints

A disposable SQL Server in Docker with the AdventureWorks sample database, plus a minimal
FastEndpoints API to build on.

## Prerequisites

- Docker (Docker Desktop on Windows/Mac)
- .NET 10 SDK
- Bash (on Windows use Git Bash or WSL), or restore the database by hand (see below)
- Optional: SSMS or Azure Data Studio to browse the data

## 1. Start SQL Server and restore the database

```bash
cp .env.example .env        # then edit .env and set your own SA password
bash scripts/restore-db.sh
```

The script downloads `AdventureWorks2025.bak` into `./backups`, starts the container, and
restores it. Re-running is safe; use `FORCE=1 bash scripts/restore-db.sh` to overwrite.

To use a different sample, set `DB_NAME` in `.env`:

| DB_NAME | Notes |
|---|---|
| `AdventureWorks2025` | Default. Transactional (OLTP) sample, good for CRUD practice. |
| `AdventureWorks2022` | Same sample, older version. |

WideWorldImporters and Contoso use different download sources, so they are not wired into the
script. Drop their `.bak` into `./backups` and restore from SSMS (the folder is mounted in the
container at `/backups`).

Start over at any time: `docker compose down -v` deletes the container and its data volume.

If the script says `sqlcmd` isn't in the image, connect with SSMS or Azure Data Studio to
`localhost,1433` (user `sa`) and restore `/backups/<name>.bak` yourself.

## 2. Add packages and set the connection string

```bash
cd src/DevApi
dotnet add package FastEndpoints
dotnet add package FastEndpoints.Swagger
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient

dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=localhost,1433;Database=AdventureWorks2025;User Id=sa;Password=YOUR_SA_PASSWORD;TrustServerCertificate=True"
```

The password lives in user secrets, not in the repo.

## 3. Run it

```bash
dotnet run
```

Open the Swagger page (FastEndpoints serves it at `/swagger`) and try `GET /products`.

## What's here

```
docker-compose.yml          SQL Server 2025 (Developer edition) container
scripts/restore-db.sh       download + restore the sample database
src/DevApi/Program.cs       FastEndpoints + Swagger setup
src/DevApi/Endpoints/       one class per endpoint (ListProductsEndpoint is the example)
```

## Next steps

1. Add `GET /products/{id}`, `POST`, `PUT`, `DELETE` with request DTOs and validators.
2. Move data access to EF Core for the write side; keep Dapper for read queries.
3. Add an xUnit test project using FastEndpoints' testing helpers.
4. Add JWT auth, paging/filtering, and a global exception handler.
5. Containerize the API and add it to `docker-compose.yml`.
