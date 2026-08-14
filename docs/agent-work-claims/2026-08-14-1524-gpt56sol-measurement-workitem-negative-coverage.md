# Work claim — Measurement work-item coverage rejects negative physical quantities

- Status: `COMPLETED`
- Agent: `gpt56sol-measurement-workitem-negative-coverage-20260814-1524`
- Registered: `2026-08-14T15:24:00+07:00`
- Workstream: `Measurement / work-item coverage integrity`
- Priority: `P1`
- Baseline observed main SHA: `f3bd1d722d7242d427f0d69c751bc21452697607`
- Pre-write source blob: `b1e9954493993daef6d9f18c8589b3a743c050ca`
- Pre-write smoke blob: `0062b795e8c9dc2451e7c17fba43ac73a9a6555e`
- Claim commit: `1839e5ccca8cf32897e83286970ce6896932cf96`
- Source fix commit: `e966417f6882713c5c92d0198e025c4bf3510f76`
- Regression commit: `d5cbb5a6ae0913c98133e5535742b659f2e68912`

## Confirmed defect

`MeasurementWorkItemCoverageEvaluator.SnapshotQuantities()` failed closed for non-finite quantity corruption and canonicalized signed zero, but accepted finite negative values from the public `ProjectElement.Quantities` dictionary as normal coverage input.

QS3D's established physical-measurement contract is finite and non-negative; signed values are reserved for revision/delta mathematics. Because the project quantity dictionary remains publicly mutable and persisted/legacy/corrupt state can bypass `SetQuantity()`, coverage must independently reject negative physical payloads instead of reporting them as ready/mapped quantities.

## Reserved scope

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs` — quantity snapshot validation only.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs` — focused negative-corruption regression only.
- this claim file.

## Implemented change

- `SnapshotQuantities()` now rejects finite `item.Value < 0d` with `InvalidOperationException`.
- Existing non-finite rejection is unchanged.
- Existing `-0d` acceptance/canonicalization remains unchanged because negative zero does not satisfy `< 0d`.
- Detached findings, ordering, mapping resolution, and stale/unmapped issue semantics remain unchanged.

## Regression

`MeasurementWorkItemCoverageSmoke.CorruptProjectStateFailsClosed()` now directly injects `-double.Epsilon` into `ProjectElement.Quantities` and proves evaluation fails closed. The existing signed-zero regression remains intact and continues to specify accepted zero semantics.

## Explicit non-scope

- no `ProjectElement.SetQuantity()` edit while another ACTIVE claim reserves `src/QS3D.Core/Domain/ProjectElement.cs`;
- no revision/delta math changes;
- no mapping-catalog identity changes;
- no persistence/schema migration;
- no UI/native work.

## Validation performed

- GitHub remote readback on current `main` confirmed the negative guard in `MeasurementWorkItemCoverage.cs` and the `-double.Epsilon` regression in `MeasurementWorkItemCoverageSmoke.cs`.
- Ancestry/readback after the regression confirmed later concurrent commits did not touch either reserved source/test path.
- Local managed build/smoke was **not executed**: this environment has no `dotnet`, `csc`, `mcs`, `msbuild`, `xbuild`, or `mono` executable.
- No GitHub Actions were dispatched.
- No licensed BricsCAD/native validation was performed or claimed for this managed-only lane.
