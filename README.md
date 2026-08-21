# Hasta Memnuniyet Anketi Sistemi

ASP.NET Core MVC + Razor Views implementation using layered projects, EF Core, and PostgreSQL.

## Structure

- `src/PatientSurvey.Domain`: entities and enums only.
- `src/PatientSurvey.Application`: DTOs, interfaces, and business workflows.
- `src/PatientSurvey.Infrastructure`: EF Core `AppDbContext`, configurations, migrations, repositories, clock, and password hashing.
- `src/PatientSurvey.WebUI`: MVC controllers, Razor views, authentication, authorization, and DI composition.
- `tests/PatientSurvey.UnitTests`: isolated Application tests.
- `tests/PatientSurvey.IntegrationTests`: EF model and MVC authorization integration checks.

The separate functional requirements document referenced by the development instructions was not present in this workspace. Features whose behavior depends on that document, especially detailed admin CRUD and reporting/export behavior, are scaffolded but intentionally not invented.

## Configuration

The application reads PostgreSQL from `ConnectionStrings:DefaultConnection`.

For Docker Compose, create a local `.env` from `.env.example` and set a real local password. Do not commit `.env`.

## Database

Create a migration:

```bash
dotnet ef migrations add MigrationName --project src/PatientSurvey.Infrastructure --startup-project src/PatientSurvey.WebUI --output-dir Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update --project src/PatientSurvey.Infrastructure --startup-project src/PatientSurvey.WebUI
```

Reset a local development database by removing the Docker volume:

```bash
docker compose down -v
```

## Run

```bash
dotnet restore PatientSurvey.slnx
dotnet build PatientSurvey.slnx
dotnet test PatientSurvey.slnx
```

Run with Docker:

```bash
docker compose up --build
```

The WebUI listens on `http://localhost:8080` and connects to PostgreSQL through the Compose service name `db`.
