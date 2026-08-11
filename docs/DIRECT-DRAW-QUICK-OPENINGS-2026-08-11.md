# QS3D Direct Draw — Quick Door / Opening

Updated: 2026-08-11 (UTC+7)

## Goal

Reduce the normal Door / WallOpening authoring path to geometry + automatic host resolution while keeping explicit per-operation dimensions available for exceptions.

The two picked edge points remain authoritative for `WidthM`. The other normal values come from the active compatible **Family / Type**.

## Quick Door

Primary command: `QS3DDRAWDOOR`

```text
Vẽ Cửa
-> pick edge 1
-> pick edge 2
-> WidthM from picked geometry
-> use Family Height / Sill / BooleanClearance
-> semantic capture
-> Auto Host only this new Door
-> verified HostWallId
```

The primary quick command does not require Height / Sill / BooleanClearance prompts after the second point.

Use `QS3DDRAWDOORADV` when those values must be entered for this individual Door.

## Quick WallOpening

Primary command: `QS3DDRAWOPENING`

The workflow is the same: two picked points define the real source LINE and authoritative width, then compatible Family `HeightM`, `SillHeightM` / `BottomOffsetM` and `BooleanClearanceM` are reused directly.

Use `QS3DDRAWOPENINGADV` for one-off explicit parameter entry.

## Host and physical-cut boundary

Quick authoring does not weaken the existing host rules. Both quick and advanced paths still:

- select only the newly-created source before Auto Host;
- require one valid `HostWallId`;
- roll back the new source + semantic operation when no unique host can be established;
- regenerate semantic state before and after Auto Host.

Regeneration is **operation-scoped**. Before Auto Host, Direct Draw regenerates only the newly-created Door/WallOpening. Auto Host repairs a same-host relation when its `HostWallId`/dependency form is non-canonical and regenerates only changed openings plus the affected new/previous live hosts. The deterministic post-host pass is limited to the created opening + resolved host. Unrelated dirty project elements are intentionally left dirty for their own workflow instead of being silently regenerated or marked clean by one Door/Opening authoring action.

The **physical boolean remains explicit**. Neither quick command silently invokes `QS3DCUTSELECTEDOPENINGS` or the broader `QS3DCUTOPENINGS` path. This prevents one creation action from mutating unrelated linked openings.

## Product interaction pattern

The intended common workflow is now:

```text
choose Family / Type once
-> Vẽ Cửa / Vẽ Lỗ Mở
-> pick two points per object
-> Auto Host
```

and only exceptional objects use `QS3DDRAWDOORADV` / `QS3DDRAWOPENINGADV`.

The existing Ribbon and Full Domain Hub already call the primary command names, so they automatically use the quick paths without adding more primary UI buttons.

## Safety

This is an interaction reduction, not a replacement lifecycle. The implementation continues to use the established source-safe path:

- Model Space + planar-UCS validation;
- finite/unit-aware width from the real LINE source;
- fail-closed Family numeric validation;
- `ProjectStateSnapshot` before mutation;
- canonical `ProjectElement.SetProperty()` writes;
- deterministic, operation-scoped regeneration;
- active-DWG checks around Auto Host;
- exact source cleanup before project restore on failure;
- best-effort post-commit UI synchronization.

Existing capture commands and explicit physical-cut commands remain unchanged.

## Runtime qualification boundary

This is source/static-contract work. Exact BricsCAD V25 editor and host behavior remains under `LOCAL-008`.

Local qualification should cover:

1. `QS3DDRAWDOOR` / `QS3DDRAWOPENING`: cancel at either picked point leaves no new project/source/semantic/native residue; accepted geometry leads directly to Family defaults + Auto Host without the old numeric prompt sequence;
2. `QS3DDRAWDOORADV` / `QS3DDRAWOPENINGADV`: cancel independently at Height, Sill and BooleanClearance prompts and verify full no-residue behavior;
3. Family default continuity on an existing project, including malformed configured values failing closed;
4. valid-host, no-host and ambiguous-host outcomes, plus a deliberately non-canonical same-host property/dependency that Auto Host must repair rather than report as unchanged;
5. with an unrelated semantic element already dirty, author/link one Door or WallOpening and verify that unrelated element remains dirty while the created opening and affected host(s) are the only regeneration scope;
6. Ribbon/Domain Hub primary buttons still launch the quick commands;
7. schedule/XLSX/Locate and explicit selected-opening physical cut continue to see the created semantic objects normally;
8. multi-DWG/document-switch protection and save/reopen behavior.

The source-level scope is locked by `scripts/preflight-auto-host-scoped-regeneration.py`; this does not replace exact V25 interaction evidence.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs batch does not authorize a workflow run.
