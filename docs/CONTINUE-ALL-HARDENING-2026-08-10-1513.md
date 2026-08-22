# Continue-all hardening handoff — 2026-08-10 15:13+07

This note records the current source-level hardening batch for agents working concurrently on `QS3D-BricsCAD`. It is intentionally separate from licensed BricsCAD V25/private-DWG runtime qualification.

## Concurrency rule

- Re-fetch `refs/heads/main` before every write and again after long reads.
- Preserve concurrent changes; never force-push.
- Do not replace large shared files from stale blobs merely to land a small edit.
- `continue all` does **not** authorize GitHub Actions. Workflows remain owner/manual `workflow_dispatch` only.

## Build3D semantic selection

`SemanticReferenceHandles.MatchesSelection` now resolves:

1. authoritative `SourceHandles`;
2. Auto Room `BoundarySourceHandles` only when the Room has no explicit source, using all-handle provenance semantics;
3. host-solid aliases `GeneratedSolidHandle` and `PhysicalOpeningCutSolidHandle`.

Generated rebar/mesh/curtain-detail handles are deliberately not treated as `QS3DBUILD3D` host selections.

## Canonical `QS3DBUILD3D`

There must be exactly one command registration, owned by `Build3DCommands.cs`.

- legacy duplicate registration in `ReviewCommands.cs` was removed;
- wall batches fail closed when LINE and POLYLINE source types are mixed;
- WallPier LINE dispatches through `WallPierProfileSolidBuilder`, preserving Rectangular/Chamfered semantics;
- supported wall POLYLINE host geometry uses `PolylineWallSolidBuilder`;
- Curtain frame detail remains a dedicated `QS3DCURTAIN3D` / `QS3DCURTAINFRAMES3D` workflow rather than appending another native transaction to host `QS3DBUILD3D` without a shared rollback contract.

## Exact document lifecycle cleanup

Document cleanup is keyed by the actual BricsCAD `Document` object through `DocumentToBeDestroyed`, not by filename after destruction.

- `SelectionSyncCoordinator.Detach(document)` removes the exact selection event subscription;
- `ProjectContextCoordinator.Forget(document)` removes the exact in-memory project and unsaved-document project key;
- this avoids stale context for untitled drawings and duplicate/similar filenames.

## Semantic recapture Family repair

Recapturing an existing semantic source now only reuses its Family when the referenced Family exists and its category matches the element category.

Dangling or wrong-category `FamilyId` values are repaired through the normal category Family resolver. Floor/Zone and existing instance properties are preserved.

## Auto Host apply atomicity

`QS3DAUTOLINKHOSTS` keeps planning non-mutating and reviewable:

- ambiguous / unmatched / invalid openings remain per-opening planning outcomes;
- once planned links begin applying, `HostWallId`, dependencies, stale state, audit changes and deterministic regeneration are inside one `ProjectStateSnapshot` rollback boundary;
- a later link/regeneration failure restores the project to its pre-apply state;
- rollback failure itself is surfaced with the original error rather than hidden.

## Wall Snap semantic source metric synchronization

`QS3DWALLSNAPAPPLY` already had preview plan/source fingerprints, one CAD transaction and generated-geometry invalidation. A missing semantic contract was fixed:

- the reviewed snap plan precomputes each touched source's post-snap plan length before CAD mutation;
- each touched semantic owner must have one authoritative source handle for this operation;
- zero/non-finite resulting source segments fail before the transaction;
- after successful CAD commit, authoritative `LengthM` is synchronized before marking Geometry/Quantity dirty;
- later `QS3DREGEN`/BQ therefore consumes the new source length rather than the captured pre-snap length.

## Direct Draw P1 boundary

WallPier Direct Draw is intentionally LINE-only in the current authoring contract. The canonical host builder dispatches that LINE through the profile-aware WallPier builder. Arbitrary open-POLYLINE Direct Draw is not silently accepted because multi-segment profile/corner behavior needs a separate proven contract.

Door/Opening Direct Draw is source + semantic + safe host-link authoring. Physical boolean remains explicit (`QS3DCUTOPENINGS` / guarded curved path) rather than silently cutting unrelated pending openings.

## Reviewed without additional mutation

Straight/straight-polyline opening booleans currently prepare ownership/fingerprint/cut plans and execute the batch `BoolSubtract` operations inside a single CAD transaction. A cutter failure therefore aborts that CAD transaction instead of intentionally committing a half-cut batch. Do not add duplicate outer CAD rollback unless a concrete failing path is demonstrated.

## Known source blocker still open

### Manual `QS3DLINKHOST` mutation + regeneration atomicity

The current legacy/manual command path still calls roughly:

```text
HostLinkService.LinkOpening(...)
RegenerateProject(project)
```

without a command-level `ProjectStateSnapshot` spanning both operations. If deterministic regeneration throws after the relationship mutation, the manual command can retain a partially committed semantic host-link state.

Preferred fix when `Commands.cs` is safe to edit:

1. re-fetch exact current `main` and current `Commands.cs` blob;
2. capture `ProjectStateSnapshot` immediately before `HostLinkService.LinkOpening`;
3. include both link mutation and `RegenerateProject(project)` in the try block;
4. restore on failure and surface AggregateException if restore also fails;
5. keep UI refresh/status after successful semantic commit only;
6. add a small static preflight guarding ordering.

Do **not** full-replace a stale `Commands.cs` while concurrent agents are editing it merely to land this fix.

## Static guards added in this hardening chain

- `scripts/preflight-semantic-reference-selection.py`
- `scripts/preflight-document-lifecycle.py`
- `scripts/preflight-semantic-recapture-family.py`
- `scripts/preflight-build3d-canonical.py`
- `scripts/preflight-auto-host-atomic.py`
- `scripts/preflight-wall-snap-source-metrics.py`

`preflight-all.py` auto-discovers `preflight-*.py`; no workflow edit is required just to register these gates.

## Runtime qualification remains separate

Do not claim source review as proof of:

- exact compile against the installed BricsCAD V25 `BrxMgd.dll` / `TD_Mgd.dll` set;
- NETLOAD/DemandLoad and real command registration;
- Direct Draw prompt/cancel/rollback behavior under interactive V25;
- WallPier profile behavior in private DWGs;
- Door/Opening host link + straight/curved physical booleans;
- Curtain LINE/open-bulged-POLYLINE frames and live fingerprint behavior;
- save/reopen/multi-DWG regression;
- Unicode/HiDPI screenshot parity;
- production signing/update/licensing infrastructure;
- multi-owner L/T/X wall-solid physical union/reconciliation.

The correct current wording remains **source-implemented / statically guarded where noted; licensed V25 runtime qualification pending**.
