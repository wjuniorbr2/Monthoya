# Monthoya

Monthoya is a Windows desktop software project for a real estate agency. The first version is built with C#/.NET and is prepared for PostgreSQL through Supabase.

## Solution Structure

- `Monthoya.Desktop` - WPF Windows desktop application.
- `Monthoya.Api` - ASP.NET Core Web API placeholder for future backend work.
- `Monthoya.Core` - Shared domain and application primitives.
- `Monthoya.Data` - Data access layer placeholders and database configuration types.

## Configuration

Do not commit real Supabase credentials, PostgreSQL passwords, or connection strings.

Use one of these approaches for local configuration:

- User secrets for development.
- Local environment variables.
- Local-only configuration files that are ignored by Git.
- Never commit digital certificates such as `.pfx`, `.p12`, `.pem`, or private key files.

The committed `appsettings.json` files contain empty placeholders only.

## Initial Setup Commands

```powershell
dotnet restore
dotnet build
```

## Database

The data layer uses Entity Framework Core with PostgreSQL. The same connection string shape can point to Supabase PostgreSQL, a local PostgreSQL server, or a future VPS/cloud PostgreSQL server.

For local API development, prefer user secrets or environment variables:

```powershell
dotnet user-secrets init --project .\Monthoya.Api
dotnet user-secrets set "Database:ConnectionString" "Host=<host>;Port=5432;Database=<database>;Username=<username>;Password=<password>" --project .\Monthoya.Api
```

For the WPF desktop app, configure the desktop project too:

```powershell
dotnet user-secrets set "Database:ConnectionString" "<connection-string>" --project .\Monthoya.Desktop
```

Restore local EF tooling before creating migrations:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add MigrationName --project .\Monthoya.Data --startup-project .\Monthoya.Data --output-dir Migrations
```

The API includes:

- `/health` - app health check.
- `/health/database` - database connectivity check. It returns `503` until a local connection string is configured.

## Desktop Foundation

The desktop app starts in Portuguese and routes through:

- database configuration guidance when no connection string is available;
- first-run administrator setup when no users exist;
- login with local password hashing;
- a role-aware shell with dashboard, user management, and developer diagnostics;
- a Paranavaí map area using WebView2 + OpenStreetMap/Leaflet for available rental properties with coordinates.

Roles:

- `Administrador` - user management and business operations.
- `Usuário` - regular business use without user management.
- `Desenvolvedor` - administrator access plus diagnostics.

## Notes

- Bank boleto integration is intentionally not implemented yet.
- Nota fiscal integration is intentionally not implemented yet.
- The API currently exposes only simple health-check endpoints.
- Future boleto, NFS-e, and file storage integrations are represented by interfaces in `Monthoya.Core` and can be implemented later.
