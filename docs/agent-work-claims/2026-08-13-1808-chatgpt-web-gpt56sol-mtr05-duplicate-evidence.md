# Work claim — MTR-05 MeasurementTrace duplicate evidence integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr05-duplicate-evidence-20260813-1808`
- Workstream: `MeasurementTrace / MTR-05` — fail closed on exact duplicate trace evidence
- Claimed UTC: `2026-08-13T11:08:00Z`
- Last updated UTC: `2026-08-13T11:08:00Z`
- Baseline main SHA: `b75c2cc2a8b8ec5934f749eb6cb11ea2b8676522`

## Confirmed defect

Current `MeasurementTraceContract.SnapshotFacts()` and `SnapshotAdjustments()` validate null entries, sort deterministically and freeze the snapshots, but accept structurally identical duplicate entries. The same contract already rejects duplicate warnings/assumptions, and `MeasurementSnapshot` rejects duplicate measurement identities. Exact duplicate adjustments therefore allow redundant/ambiguous explanatory evidence in the canonical trace while neighboring canonical structures fail closed on duplicates.

## Reserved files

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file

## Scope

- After canonical sort, reject adjacent structurally identical facts and adjustments with `ArgumentException`.
- Preserve ordering, equality/hash/canonical serialization, MTR1/MTR2 schema behavior, quantity values, units and all calculation ownership for non-duplicate evidence.
- Add focused smoke regression for duplicate fact and duplicate adjustment rejection.
- Do not touch Wall/MTR-03R, Takeoff/report/UI, persistence, BricsCAD/native, or formula/calculation surfaces.

## Initial overlap check

- Current source/test readback confirms the duplicate-evidence gap still exists.
- Historical MeasurementTrace nullable lane is `COMPLETED` and previously reserved these files only for nullable-contract alignment.
- MTR-03R Wall Quantity trace projection is `COMPLETED` and reserved `WallQuantityCalculator.cs`, its own smoke and registration, not either file in this claim.
- Recent visible claim commits are UI/native/CST/other bounded lanes; no visible ACTIVE/BLOCKED reservation of `MeasurementTrace.cs` or `MeasurementTraceContractSmoke.cs` was found before this claim.

## Validation plan

- Re-fetch `main` after this claim-only commit and recheck overlap before source changes.
- Add exact duplicate regressions while preserving distinct same-source evidence.
- Reconcile against current `main` immediately before write; no force-push.
- Read back final files and compare the implementation commit.
- Do not dispatch GitHub Actions and do not claim managed/native PASS without execution.

## Completion

Pending implementation.
