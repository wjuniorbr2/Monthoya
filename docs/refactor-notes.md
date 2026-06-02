# Refactor notes

This file records small, tested refactors that should be preserved during future changes.

## Current stable checkpoint

The app has a working baseline after the `Pessoas` and `Imóveis` tab-reset/media fixes.

Tested behaviors:

- `Pessoas` opens cleanly in a fresh tab.
- `Imóveis` opens cleanly in a fresh tab.
- Returning to an existing tab keeps that tab's selected record/state.
- `Imóveis` save/edit stays on the same property after saving.
- `Imóveis > Fotos e arquivos` supports:
  - image preview;
  - zoom buttons;
  - Ctrl + mouse wheel zoom;
  - left-click drag/pan in the preview window;
  - media caption/legenda display;
  - edit-only remove button;
  - database record removal for saved media.

## Shared helpers added

### `ShellWindow.RefreshFeedback.cs`

Centralizes visual feedback for buttons whose text is `Atualizar`.

Behavior:

1. `Atualizar`
2. `Atualizando...`
3. `Atualizado`
4. back to `Atualizar`

This should avoid duplicating refresh feedback code in every module.

### `ShellWindow.FreshTabReset.cs`

Provides reusable fresh-tab reset logic.

Purpose:

- Prevent a fresh tab from inheriting selected records/form data from another tab.
- Keep the existing state when the tab already has state for that page.

### `ShellWindow.FreshTabResetRegistration.cs`

Registers fresh-tab reset behavior currently for:

- `Pessoas`
- `Imóveis`

Future modules with the same state-leak issue should be added here instead of creating one-off runtime patch files.

## Important design note

The current WPF shell still uses shared visual controls for modules. Tabs are separated by saved page state, not by separate page instances.

Because of this, when adding new modules or changing tab behavior, avoid relying only on visible tab labels. Each module should either:

1. capture/restore its page state correctly; or
2. register a fresh-tab reset using `RegisterFreshTabReset`.

## Remaining technical debt

### Imóveis media deletion

Saved media deletion currently removes the database record, but the physical file in local/Supabase storage is not deleted yet.

This is intentional for now because it avoids accidental loss of files that may still be referenced or needed for history/backups.

A future cleanup should move media deletion into the proper `IRentalManagementService` / `RentalManagementService` layer and decide whether physical file cleanup should be:

- immediate hard delete;
- soft delete / inactive status;
- delayed cleanup job;
- never deleted automatically.

### Imóveis runtime media file

`ShellWindow.ImoveisRuntimeFixes.cs` still contains several working behaviors:

- safer property selection handling;
- save behavior that stays on the saved property;
- media preview binding;
- media captions;
- edit-only media remove button;
- preview window with zoom.

Do not remove this file until its behavior is moved carefully into normal `Imóveis` code or smaller focused files and tested after each step.

## Recommended next refactors

Do these one at a time:

1. Move saved `Imóvel` media deletion from WPF/runtime helper code into `IRentalManagementService` and `RentalManagementService`.
2. Decide and implement file-storage cleanup policy for removed media.
3. Split `ShellWindow.ImoveisRuntimeFixes.cs` into smaller files:
   - selection/save behavior;
   - media card actions;
   - preview window behavior;
   - media deletion.
4. Apply fresh-tab reset to other modules only if they show the same state-leak behavior.
