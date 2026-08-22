# QS3D-BricsCAD — full repository audit & hardening plan

Audit date: 2026-08-10 (UTC+7)

This document records the source-level audit performed after the Foundation Mesh integration. It is intentionally stricter than a feature checklist: the goal is to preserve project data, CAD ownership, deterministic quantities and release-gate correctness while multiple agents are changing `main` concurrently.

## 1. Audit priorities

### P0 — compile/data-loss blockers

1. **Build graph must expose exactly one generated-ownership health facade.** A stale `Compile Remove` after deleting the old shim would make `GeneratedHandleOwnershipHealthService` disappear from Core compilation. Concurrent `main` fixed this during the audit by removing the stale `Directory.Build.targets` exclusion; the current preflight must keep that invariant.
2. **Semantic capture must be transactional.** Creating a Family/Element, replacing CAD-derived metrics and regenerating quantities may throw. A failed capture must restore the complete `ProjectState`, including Families, Elements, Rules, Audit, Metadata and timestamps.
3. **QS3D-generated CAD must never become a new semantic source.** Generated host, opening-cut, rebar, mesh and curtain handles are outputs. Capturing them as input creates a feedback loop and corrupts ownership/quantity semantics.
4. **Multi-selection and room-finish batches must not leave partial project mutations.** If one item fails, the whole logical operation must restore its pre-operation state.

### P1 — cross-feature consistency

1. Generated ownership classification must have one source of truth. Core now owns the rule: `PhysicalOpeningCutSolidHandle` and every `Generated*Handle` / `Generated*Handles` property are owner slots; reference/provenance keys such as `HostHandle` are not.
2. Selection, B4D exclusion, ownership health, BOM release validation, semantic capture and release readiness must consume the same owner-slot contract instead of maintaining family lists.
3. `QS3DRELEASECHECK` must be at least as strict as the generated-rebar portion of `QS3DHEALTHALL`: Foundation Mesh health and generated mode/category metadata are release blockers, not optional diagnostics.
4. Ambiguous generated ownership must fail closed. Future generated families must work without editing selection/B4D lists.

### P2 — runtime/product completion

These are not safe to mark complete from source review alone:

- compile the exact final SHA against the installed BricsCAD V25 managed assemblies;
- DemandLoad/NETLOAD, command/Ribbon/palette smoke tests in licensed Windows BricsCAD V25;
- save/reopen/multi-DWG/private-DWG regression for wall, room, opening, curtain, structure, BQ/BBS and all generated rebar families;
- real screenshots for Unicode/HiDPI and BLT-like workspace comparison;
- physical wall-solid reconciliation at L/T/X/Multi junctions;
- broader curved/open-polyline curtain frame support and panel-by-panel glass solids;
- fabrication-grade rebar hooks, bend radii, anchorage and code-specific detailing;
- Authenticode production signing/updater and optional commercial licensing/backend.

## 2. Implemented hardening in this audit batch

### Shared generated ownership registry

`QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy` is now public and provides:

- `IsOwnerSlot` — one classification rule;
- `EnumerateOwnerHandles` — normalized/deduplicated handles with their owner slot;
- `CollectOwnerHandles` — project-wide owner output set;
- `TryFindOwner` — fail-closed generated owner lookup with ambiguity detection.

The BricsCAD adapter keeps only a compatibility facade delegating to Core; it does not duplicate classification logic.

`SemanticHandleOwnershipResolver`, safe ownership health, BOM release guards and Release Readiness consume the shared registry. A future property such as `GeneratedFuturePanelHandles` is therefore automatically recognized without editing a list.

### Semantic capture safety

`SemanticCaptureService` now:

- rejects a selected handle when it is already a QS3D generated-owner output;
- performs the generated-output check before adding/mutating semantic elements;
- snapshots the full project before a single recognition/manual `CaptureSnapshot`;
- snapshots the full project before a multi-selection capture batch;
- restores the snapshot if conversion/regeneration fails;
- escalates an aggregate error if the original operation and rollback both fail.

Recognition/B4D already applies results through `CaptureSnapshot`, so the same guard covers interactive recognition and auto-accepted B4D results.

### Room-finish atomicity

`GenerateRoomFinishes` and `SyncExistingRoomFinishes` now use the same `ProjectStateSnapshot` rollback pattern. A failure while generating/synchronizing one of the five finish families no longer leaves earlier finish elements partially committed in memory.

### Release Readiness parity

`QS3DRELEASECHECK` now includes:

- Foundation Mesh live/count/metadata/category/stale health;
- generated rebar mode/category metadata health;
- project-wide owner handles from the shared registry;
- existing model/source health, safe ownership, longitudinal/shape/tie/stirrup/slab/wall/curtain health, live curtain checks, stale health and BOM release guards.

A source-level READY result still does **not** replace the licensed V25/private-DWG runtime gate.

## 3. Regression and static gates

The batch expands `SemanticHandleOwnershipSmoke` to cover:

- current generated multi-handle families;
- Foundation Mesh;
- an unknown future `Generated*Handles` family;
- non-owner reference metadata such as `HostHandle`;
- case-insensitive owner-handle deduplication;
- `PhysicalOpeningCutSolidHandle` ownership;
- ambiguous generated ownership rejection.

Static gates were updated/added for:

- dynamic semantic selection ownership;
- B4D generated-source exclusion using the Core contract;
- canonical ownership compilation/enumeration;
- Release Readiness Foundation/mode/BOM parity;
- transactional semantic capture and room-finish rollback.

`scripts/preflight-all.py` discovers `preflight-*.py` automatically, so the new gate requires no workflow change.

## 4. Validation boundary for this audit

The source branch was reviewed through GitHub compare/file APIs. The execution container could not resolve `github.com`, so it could not clone the repository; therefore this audit does **not** claim a local `dotnet build`, Core smoke execution, Python aggregate preflight pass or BricsCAD runtime pass.

GitHub Actions remain manual-only by repository policy. `continue all`, commits, merges and this audit do not authorize a workflow dispatch.

## 5. Merge discipline

Before merging this batch:

1. fetch `main` again;
2. compare every commit added after the branch base;
3. if target files overlap, rebase/union changes rather than choosing ours/theirs wholesale;
4. review the final changed-file set;
5. open a PR and require GitHub mergeability;
6. squash merge without force;
7. verify the final `main` SHA and confirm no unexpected Actions run was created for that SHA.

The canonical rule remains: newer concurrent source wins unless the audit explicitly reapplies a missing safety invariant on top of it.
