# Measurement Work-Item Coverage Export

This runbook defines the source-side contract for exporting the measurement/work-item coverage matrix.

## Snapshot boundary

A coverage report is derived from a `ProjectState` and a `MeasurementWorkItemMappingCatalog`. Callers that need a traceable artifact must create the matrix with `MeasurementWorkItemCoverageMatrix.Create(project, report)`. That overload captures an immutable `MeasurementWorkItemCoverageProvenance` at matrix-creation time.

The captured source fields are:

- `ProjectId` — canonical QS3D project identity;
- `DrawingFingerprint` — drawing identity when available, otherwise an empty string;
- `ChangeVersion` — project mutation version at the snapshot boundary;
- `UpdatedUtc` — UTC project timestamp at the same boundary.

The matrix owns copied scalar values. Later edits to the live `ProjectState` must not alter an already-created matrix or its exported CSV.

## CSV compatibility

`MeasurementWorkItemCoverageCsvExporter` keeps the legacy ten-column CSV contract for matrices created with `Create(report)`. A project-aware matrix appends four columns after those legacy columns:

`SourceProjectId,SourceDrawingFingerprint,SourceChangeVersion,SourceUpdatedUtc`

The legacy columns retain their existing order and values. Provenance text uses the same CSV quoting and formula-injection hardening as coverage values. `ChangeVersion` uses invariant integer formatting and `UpdatedUtc` uses the invariant round-trip (`O`) UTC representation.

## Determinism and safety

For a given matrix snapshot, repeated exports are deterministic even if the original project is mutated later. Existing deterministic matrix ordering, strict UTF-8 validation, temporary-file commit, and CSV formula-injection guards remain required.

Do not substitute local time, file modification time, branch SHA, machine name, private DWG paths, or licensed-runtime claims for source provenance. This contract is repository-safe and can be verified by Core smoke tests.

## Verification

The source acceptance gate is:

1. Core compiles without warnings-as-errors regressions.
2. `MeasurementWorkItemCoverageCsvProvenanceSmoke` passes legacy compatibility, snapshot detachment/determinism, CSV injection guard, and fail-closed validation checks.
3. Repository preflight and Core required checks pass on the exact PR head reconciled with current `main`.

Licensed BricsCAD execution is not required for this export contract.
