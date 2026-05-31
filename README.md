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
