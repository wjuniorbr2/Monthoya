# Monthoya Codex Instructions

These instructions apply to this repository unless the user gives a more specific direction in the current turn.

## Default Workflow

- Keep changes narrow and scoped to the user request.
- Prefer existing Monthoya patterns over new abstractions.
- Do not inspect broadly when a small UI or copy change has an obvious target.
- Use Portuguese BR for user-facing desktop text.
- Never hardcode API keys, database passwords, Supabase keys, bank credentials, certificate data, or other secrets.
- Preserve unrelated user changes. Do not revert files unless the user explicitly asks.

## Validation Defaults

- For UI-only copy, spacing, sizing, or layout tweaks: skip tests by default; run a build only if the change touches C# logic or compile-sensitive code.
- For C# behavior changes: run `dotnet build Monthoya.Desktop\Monthoya.Desktop.csproj`.
- For data/service/OCR/business logic changes: run build and `dotnet test Monthoya.sln --no-restore`.
- If `Monthoya.Desktop` is running and locks build DLLs, stop only the `Monthoya.Desktop` process and retry the build.
- Mention clearly when validation is skipped by these defaults.

## Git Defaults

- After completing requested code changes, commit and push to `origin/main` unless the user says not to.
- Use concise commit messages.
- Before committing, run `git status -sb` and stage only files related to the request.
- Commit/push are still expected even when tests are skipped, as long as the change is complete.

## Monthoya Product Guardrails

- Monthoya is a one-company real estate management system first, not SaaS.
- Keep business rules in `Monthoya.Core`, data access in `Monthoya.Data`, and WPF UI logic in `Monthoya.Desktop`.
- Optimize first for rental administration: people, documents, properties, contracts, payments, owner repasses, and operational workflows.
- Treat integrations like boleto, PIX, NFS-e, bank APIs, certificates, mobile API, and accounting exports as future replaceable interfaces unless explicitly requested.

## Desktop UI Defaults

- Keep WPF screens dense, practical, and consistent with the existing card/table/form style.
- Avoid oversized decorative UI.
- For small visual adjustments, change only the relevant layout/style code and avoid broad refactors.
- Make important action buttons visually distinct when they compete with dense form text.
