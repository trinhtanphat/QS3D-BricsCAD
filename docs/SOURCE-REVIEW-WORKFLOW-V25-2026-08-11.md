# QS3D Review Command Workflow — source handoff 2026-08-11

This batch wires the existing Core dependency-impact and preview-review contracts into the BricsCAD V25 command adapter without turning source evidence into runtime qualification.

## Command surface

Existing commands keep their prior whole-project behavior:

- `QS3DRULEPREVIEW` — whole-project quantity-rule preview, read-only.
- `QS3DREGENPREVIEW` — whole-project regeneration preview, read-only.
- `QS3DDIAGSUMMARY` — diagnostic summary export.

New source-safe review commands:

- `QS3DIMPACTPREVIEW` — uses the current PICKFIRST selection, resolves selected native entities through `SemanticSelectionResolver`, and prints a bounded semantic dependency-impact summary (`root`, `cause`, `depth`).
- `QS3DREGENPREVIEWSEL` — previews regeneration only for the semantic elements resolved from the current PICKFIRST selection.
- `QS3DRULEPREVIEWEXPORT` — exports the current whole-project Quantity Rule Preview as a fingerprinted `.qsreview` snapshot.
- `QS3DREGENPREVIEWEXPORT` — exports the current whole-project Regeneration Preview as a fingerprinted `.qsreview` snapshot.
- `QS3DREGENPREVIEWEXPORTSEL` — exports a semantic-selection-scoped Regeneration Preview snapshot.

## Read-only and freshness boundaries

The command adapter does not call guarded Apply, live regeneration, or native write transactions.

Selection-scoped commands reuse `Cad.SemanticSelectionResolver.ResolveImplied(document, project)`. That adapter reads PICKFIRST entities with an open/close read transaction and resolves their handles through `SemanticHandleOwnershipResolver`, so generated/native owner ambiguity remains governed by the shared ownership contract rather than duplicated command code.

Review export confirms the destination before creating the preview/snapshot. The exported artifact is then created by `PreviewReviewSnapshotService` and persisted by `PreviewReviewSnapshotStore`, preserving the existing fingerprint, invariant validation, atomic replacement, file-size, XML/DTD, and CAD-handle filtering rules.

A `.qsreview` file is a review artifact only. It is not an Apply instruction, a native-geometry payload, or authoritative CAD ownership state.

## Regression gate

`scripts/preflight-review-impact-commands.py` checks that:

- the new command names and semantic-selection wiring exist;
- impact planning uses `DependencyImpactPlanner`;
- selection regeneration uses `RegenerationPreviewService.PreviewSubset`;
- review exports use the existing snapshot service/store;
- Save confirmation precedes preview creation and file persistence;
- command code contains no Apply, live `RegenerateDirty`, native `ForWrite`, or write-transaction path;
- semantic selection remains read-only and ownership-aware;
- the Core dependency-impact and preview-review preflights still pass when the aggregate preflight is run.

`preflight-all.py` already auto-discovers `preflight-*.py`; no aggregate runner edit is needed.

## Qualification status

Status for this batch is `LANDED_SOURCE` only after integration to `main`.

It does **not** claim:

- successful BricsCAD V25 compile against the private/local SDK;
- successful `NETLOAD` or command execution in a licensed V25 session;
- PICKFIRST behavior across real customer DWGs;
- exact-SHA multi-document/runtime qualification;
- native geometry/boolean/ownership mutation qualification;
- clean-machine installer/signing/release readiness.

Those remain governed by the repository's existing `LOCAL_ONLY` exact-SHA qualification process.
