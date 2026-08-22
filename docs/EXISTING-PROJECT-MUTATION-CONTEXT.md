# Existing project mutation context

Updated: 2026-08-11 (UTC+7)

QS3D has three different project-resolution intents. They must not be interchanged.

## 1. Creation-capable workflows

`ProjectContextCoordinator.GetOrCreate(document)` is reserved for commands whose product contract explicitly permits creating a new QS3D project. It may load and bind an existing `.qsdb`, or create a default in-memory project when no sidecar exists.

Do not use `GetOrCreate` from a stale/modeless callback, a remove/refresh command, or another operation whose meaning requires a pre-existing semantic owner/project.

## 2. Read-only inspection

`ProjectContextCoordinator.TryGetReadOnly(document, out project)` is for Health, Locate, preview and other non-mutating inspection. With a cold cache it may load a validated detached snapshot from disk without binding that instance into the live project cache.

A detached read-only snapshot must never be passed into a builder/service that mutates `ProjectState`, metadata, generated ownership, audit, dirty state or semantic elements.

## 3. Existing canonical mutation

`ExistingProjectMutationContext.TryGet/Require` is the adapter boundary for an operation that must mutate an already-existing project but must not create one.

The resolver:

1. proves an existing project/sidecar with the non-creating read-only path;
2. resolves through `GetOrCreate` only after that proof so the returned mutable instance is the coordinator-owned canonical cached project;
3. verifies that the canonical `ProjectId` still matches the observed project;
4. forgets the accidentally-created/replaced cache entry and fails closed if the project changes between probe and bind;
5. lets corrupt sidecars or drawing-identity mismatches fail closed before the caller receives a mutable project.

This avoids two distinct bugs:

- **detached mutation** — native Table/tag/Auto Host code mutates a cold-cache snapshot that is not the live canonical project;
- **accidental project creation** — a stale Recognition callback or ownership-dependent command silently creates a new empty project after the original project is unavailable.

## Current migrated mutation paths

The current source contract covers:

- generic Semantic Element native Table Build/Refresh/Remove;
- BQ native Table Build/Refresh/Remove;
- BBS native Table Build/Refresh/Remove;
- Material Usage native Table Build/Refresh/Remove;
- Room Finish native Table Build/Refresh/Remove;
- Door/Opening native Table Build/Refresh/Remove;
- `QS3DAUTOLINKHOSTS` semantic host-link mutation;
- Recognition modeless Apply and skip-audit callback;
- Semantic Tag create/refresh/remove.

Their Health/Locate paths intentionally remain on `TryGetReadOnly` where no mutation is required.

## Failure and persistence expectations

- No existing project/sidecar: mutation command fails/returns without creating a project.
- Existing project in cache: the exact cached instance is used.
- Cold cache + valid `.qsdb`: command rebinds a canonical project before mutation; subsequent save/palette/project operations see the same object.
- Corrupt/mismatched `.qsdb`: fail closed; do not replace it with a default project for the mutation.
- Sidecar disappears/replaces between probe and bind: do not expose the newly-created/replaced project to the mutation caller.
- Read-only Health/Locate remains non-creating and may stay detached on a cold cache.

Static enforcement is in `scripts/preflight-existing-project-mutation-context.py`.

## LOCAL_ONLY qualification

Licensed V25 qualification must cover a cold-cache reopen with an existing `.qsdb`, mutation followed by save/reopen, absent-sidecar refusal, drawing switch/multi-DWG isolation, stale Recognition Apply, native Table metadata persistence, and Semantic Tag ownership persistence. The exact runnable scenarios are indexed in `docs/LOCAL-AGENT-INBOX.md`.
