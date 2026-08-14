# Work claim — Measurement work-item coverage rejects negative physical quantities

- Status: `ACTIVE`
- Agent: `gpt56sol-measurement-workitem-negative-coverage-20260814-1524`
- Registered: `2026-08-14T15:24:00+07:00`
- Workstream: `Measurement / work-item coverage integrity`
- Priority: `P1`
- Baseline observed main SHA: `f3bd1d722d7242d427f0d69c751bc21452697607`
- Pre-write source blob: `b1e9954493993daef6d9f18c8589b3a743c050ca`
- Pre-write smoke blob: `0062b795e8c9dc2451e7c17fba43ac73a9a6555e`

## Confirmed defect

`MeasurementWorkItemCoverageEvaluator.SnapshotQuantities()` fails closed for non-finite quantity corruption and canonicalizes signed zero, but it accepts finite negative values from the public `ProjectElement.Quantities` dictionary as normal coverage input.

QS3D's established physical-measurement contract is finite and non-negative; signed values are reserved for revision/delta mathematics. Because the project quantity dictionary remains publicly mutable and persisted/legacy/corrupt state can bypass `SetQuantity()`, coverage must independently reject negative physical payloads instead of reporting them as ready/mapped quantities.

## Reserved scope

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs` — quantity snapshot validation only.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs` — focused negative-corruption regression only.
- this claim file.

## Intended change

- Reject finite `item.Value < 0d` in the coverage snapshot with `InvalidOperationException`.
- Preserve existing non-finite rejection.
- Preserve `-0d` acceptance and canonicalization to positive zero (`-0d < 0d` is false).
- Preserve detached findings, ordering, mapping resolution, and stale/unmapped issue semantics.

## Regression plan

Extend the existing corruption smoke by directly injecting a small finite negative value into `ProjectElement.Quantities` and proving `MeasurementWorkItemCoverageEvaluator.Evaluate()` fails closed. Keep the existing signed-zero regression unchanged to prove zero semantics remain accepted.

## Explicit non-scope

- no `ProjectElement.SetQuantity()` edit while another ACTIVE claim reserves `src/QS3D.Core/Domain/ProjectElement.cs`;
- no revision/delta math changes;
- no mapping-catalog identity changes;
- no persistence/schema migration;
- no UI/native work;
- no GitHub Actions dispatch or licensed BricsCAD qualification.

## Validation boundary

Remote GitHub diff/readback and ancestry verification only unless an executable .NET toolchain is independently available. No GitHub Actions will be dispatched, and no fresh managed/native PASS will be claimed without execution evidence.
