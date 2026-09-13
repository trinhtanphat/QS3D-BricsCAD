# C03 Workspace viewport-aid document-generation affinity

## Scope

This runbook covers the V25 modeless Workspace viewport-aid controls in `WorkspacePanel.ViewAids.cs`: light/contrast `BKGCOLOR`, `ORTHOMODE`, `OSMODE` enable/suppress, per-mode Entity Snap menu changes, and their display refresh lifecycle.

These controls are document-scoped even though BricsCAD exposes them through application-level system-variable APIs. A modeless WPF callback may pump the host, switch MDI documents, close/reload a drawing, or leave the same managed `Document` wrapper attached to a different native database generation between a native read and write.

## Required mutation contract

Every document-scoped viewport-aid mutation must:

1. Capture `MdiActiveDocument`.
2. Capture its native database generation with `DocumentGenerationGuard.CaptureCurrent`.
3. Revalidate the managed/native pair immediately before the native system-variable read.
4. Compute the requested value without publishing document-scoped UI state.
5. Revalidate the same managed/native pair immediately before `SetSystemVariable`.
6. Fail closed when either generation check fails.
7. Treat UI refresh after a successful native write as best-effort display work; a refresh failure must not be reported as a failed native mutation.

No second transaction, compensating native write, or rollback is introduced after `SetSystemVariable` succeeds.

## State-specific invariants

`BKGCOLOR` preset restore state is committed only after the generation-fenced native mutation succeeds. A failed native write or document-generation drift must not consume or replace the saved restore color.

`OSMODE` keeps configured snap bits separate from suppression bit `16384`. Enable/suppress toggles preserve configured modes; per-mode menu operations preserve the current suppression bit. A request to enable Entity Snap with no configured modes remains a no-op with explanatory UI state.

Display-only refreshes capture the active managed/native generation before reading `BKGCOLOR`, `ORTHOMODE`, and `OSMODE`, and revalidate before publishing WPF button/menu state. Stale reads are discarded rather than published for a different document generation.

## Validation boundary

REMOTE_SAFE evidence includes the auto-discovered source guard, focused Workspace/Start Center predecessor guards, broad feature-preflight discovery, protected exact-head Shared/Hybrid CI, and admitted V25 reference compile when reached by Shared CI.

Real BricsCAD modeless timing remains LOCAL_ONLY until exercised on a licensed host: MDI A→B→A, same-wrapper native database replacement, document close/reload during callbacks, visible palette state, and UI-thread/PaletteSet timing. Do not convert source/CI evidence into a licensed-runtime PASS.
