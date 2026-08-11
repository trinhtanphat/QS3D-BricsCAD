# Existing project mutation context

Updated: 2026-08-11 (UTC+7)

QS3D has three different project-resolution intents. They must not be interchanged.

## 1. Creation-capable workflows

`ProjectContextCoordinator.GetOrCreate(document)` is reserved for commands whose product contract explicitly permits creating a new QS3D project. It may load and bind an existing `.qsdb`, or create a default in-memory project when no sidecar exists.

Do not use `GetOrCreate` from a stale/modeless callback, a remove/refresh command, or another operation whose meaning requires a pre-existing semantic owner/project.

## 2. Read-only inspection and derived-data regeneration

`ProjectContextCoordinator.TryGetReadOnly(document, out project)` is for Health, Locate, preview, preference loading and other non-mutating inspection. With a cold cache it may load a validated detached snapshot from disk without binding that instance into the live project cache.

A read-only workflow that needs regenerated derived values must not call `RegenerateDirty(project)` on that returned object. It must create `ProjectStateSnapshot.CreateDetachedCopy(project)`, regenerate the copy, and build/export rows from that copy. This keeps refresh/export fresh without changing live dirty flags, audit, timestamps or semantic state merely because a user opened or exported a review window.

Current detached-regeneration examples include regeneration-based schedule exports plus Door/Opening and Room Finish modeless refresh/export and BBS modeless XLSX export.

## 3. Existing canonical mutation

`ExistingProjectMutationContext.TryGet/Require` is the adapter boundary for an operation that must mutate an already-existing project but must not create one.

The resolver first proves an existing project/sidecar through the non-creating read-only path, then resolves through `GetOrCreate` so the mutable instance is coordinator-owned, verifies that its `ProjectId` still matches the observed project, and fails closed/forgets the cache entry if the project changed between probe and bind. Corrupt or drawing-mismatched sidecars fail before a mutable project is returned.

Use this path for real writes such as native Table/tag ownership changes, Auto Host semantic links, Recognition Apply and BQ visible-column preference persistence. BQ preference loading remains read-only; only the metadata write promotes to canonical mutation context and keeps its rollback snapshot.

## Failure and persistence expectations

- No existing project/sidecar: ownership-dependent writes fail without creating a project.
- Cold cache + valid `.qsdb`: true writes bind the canonical project before mutation.
- Read-only refresh/export: read existing state, clone it, regenerate/build the clone, and leave live project state untouched.
- Corrupt/mismatched `.qsdb`: fail closed rather than replacing it with a default project.
- Sidecar disappears/replaces during true mutation binding: fail closed before exposing replacement state.
- Health/Locate/filter/preference-load and detached review/export remain non-creating.
- Save-file cancellation happens before detached copy/regeneration for pure exporters.

Static enforcement is in `scripts/preflight-existing-project-mutation-context.py` plus the focused export/modeless freshness preflights.

## LOCAL_ONLY qualification

Licensed V25 qualification must cover cold-cache existing-sidecar behavior, absent-sidecar refusal, drawing switch/multi-DWG isolation, stale Recognition Apply, native Table/tag ownership persistence, BQ preference persistence, and modeless/export detached regeneration. For the latter, record before/after live `ProjectId`, dirty/change-version/timestamp/audit indicators and prove that refresh/export obtains fresh rows without mutating live project state. The exact runnable scenarios are indexed in `docs/LOCAL-AGENT-INBOX.md`.
