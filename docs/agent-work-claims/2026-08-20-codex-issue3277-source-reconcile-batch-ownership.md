# Work claim — issue #3277 Source Reconcile batched canonical ownership

- Status: `ACTIVE`
- Lane-Key: `issue-3277`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue3277-source-reconcile-batch-ownership`
- Parent product gap: `#80`
- Baseline main SHA: `5389f67657e93c3b193c1a8e00ad75476759b16c`

## Confirmed defect

`SourceReconcileService.ResolveTargets(...)` calls
`SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(...)` once for every
selected source snapshot. Each call rechecks project element-ID uniqueness and
rescans every element/source handle. A valid selection of `S` tracked sources in
an `E`-element project therefore repeats full ownership work `S` times before
the native transaction starts. It also bypasses the canonical batch resolver's
10,000 raw-selection input ceiling.

Core already exposes `SemanticHandleOwnershipResolver.Resolve(...)`, which
materializes the bounded selection once and scans canonical source/generated
ownership once. Source Reconcile can reuse that authority without creating a
second ownership model.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs`
- `scripts/preflight-source-reconcile.py`
- `scripts/preflight-source-reconcile-single-bind.py`
- one new focused Source Reconcile batched-ownership preflight under `scripts/`
- this claim file

## Intended contract

- Reject selected QS3D-generated output before resolving authoritative sources.
- Resolve all selected source handles through one canonical bounded batch call.
- Map each returned one-source semantic owner back to the selected snapshot by
  canonical CAD handle identity without rescanning the project per snapshot.
- Preserve unknown-source refusal, exactly-one-source P0 policy,
  duplicate-selected-element refusal, read-only preview/single canonical bind,
  project/version/target freshness, invalidation, regeneration, rollback,
  Undo/Redo, audit ownership and explicit native rebuild behavior.

## Exclusions and collision boundary

- The historical `#1005` claim's latest reserved production scope is the
  `SourceReconcileUndoCoordinator` command/marker/history boundary. This lane
  does not edit that coordinator, lifecycle observers, marker storage, or any
  LOCAL-004 runner/probe/evidence surface.
- No native builder/geometry, dependency closure, invalidation semantics,
  Direct Draw, MOVE/ROTATE/STRETCH/grip/jig automation, project persistence,
  private/customer DWG, GitHub Actions, release/signing or issue `#74` work.
- Issue `#80` remains open for the broader interactive/native edit workflow.

## Validation plan

- Run the new focused gate plus all existing Source Reconcile gates.
- Run the existing Core canonical source ownership and selection-bound smokes
  through the complete Core smoke executable.
- Run generic/aggregate preflight, Core Release build and installed-reference
  BricsCAD V25 `Release|x64` build without launching BricsCAD.
- Obtain exact-head branch CI, protected current-candidate PR checks, then merge
  the one canonical PR through the protected-main path.

