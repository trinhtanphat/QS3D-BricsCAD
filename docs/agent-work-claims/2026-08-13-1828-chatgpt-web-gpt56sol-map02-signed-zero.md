# Work claim — MAP-02 coverage signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map02-signed-zero-20260813-1828`
- Registered UTC: `2026-08-13T11:28:00Z`
- Last updated UTC: `2026-08-13T11:30:00Z`
- Baseline main SHA: `f3d742f91ec8145936931cede8b8019128391bf8`
- Priority: `MTR-05 / MAP-02 P0-P1 hardening` — public quantity coverage findings must not expose IEEE negative zero

## Confirmed defect

`MeasurementWorkItemCoverageEvaluator.SnapshotQuantities()` rejected non-finite values from the public mutable `ProjectElement.Quantities` dictionary but copied every finite value unchanged into detached `MeasurementWorkItemCoverageFinding.QuantityValue`. Explicit IEEE `-0.0` therefore survived the coverage projection and remained observable by sign bit even though it is semantically zero.

The repository already treats signed-zero representation splits as quantity/unit/report canonicality defects: UnitScale and public Quantity Report output canonicalize exact zero to positive `0d`. MAP coverage is another public detached quantity projection and now preserves the same representation invariant without changing quantity math or mapping readiness.

## Reserved files

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`
- this claim file

## Implemented scope

- `SnapshotQuantities()` still rejects NaN/Infinity first, then canonicalizes every exact-zero finite value with `item.Value == 0d ? 0d : item.Value` before creating the detached quantity snapshot.
- Public `MeasurementWorkItemCoverageFinding.QuantityValue` therefore exposes positive-zero representation for both `+0.0` and `-0.0` source values.
- Added focused `SignedZeroIsCanonicalized()` smoke coverage using an explicit negative-zero sign bit injected through the public mutable quantity dictionary.
- Regression requires numeric zero plus `BitConverter.DoubleToInt64Bits(value) == 0L`, and separately proves the evaluator does not mutate the source dictionary while canonicalizing its detached finding.
- Preserved mapped/unmapped/stale/missing semantics, deterministic ordering, mapping resolution and ordinary finite quantity values.
- Did not change `ProjectElement.SetQuantity`, MAP-01 catalog, QSDB/persistence/schema, MeasurementTrace/active none-trace reconciliation, REV-03, reports/UI, rates/cost, geometry or BricsCAD/native surfaces.

## Coordination / overlap reconciliation

- Claim-only commit: `c8d8718fcc3ad78cf2a3032574fafb6782489990` — `chore(agent): claim MAP-02 coverage signed-zero canonicality`.
- Post-claim refresh showed the claim at HEAD before source write.
- Current `MTR-05 none trace reconciliation` remained a separate MeasurementTrace lane; no MeasurementTrace file was touched here.
- After the regression commit, `main` advanced once to `90b79779e8b9c6e7d0ae6120b36ecf84ebeb84e5`; compare from `959cc64a35e0cd014752f4d16124f2b7edd319d6` showed only the unrelated V25 status DockPanel claim closeout document.
- No reserved MAP source/test file was overwritten by concurrent work.

## Implementation commits

- `b8cea6f447d405a92dd4d6c2c3769147066bc961` — `fix(mapping): canonicalize coverage signed zero`.
- `959cc64a35e0cd014752f4d16124f2b7edd319d6` — `test(mapping): guard coverage signed zero`.

## Validation actually executed

- Executed: current-`main` refresh before claim, post-claim ownership recheck, post-implementation refresh and final reconciliation.
- Executed: exact GitHub commit diff inspection for source and regression; each commit changed only its single reserved implementation/test file.
- Executed: regression review confirmed explicit negative-zero construction, public finding sign-bit assertion and source-dictionary non-mutation assertion.
- Executed: GitHub compare `959cc64a35e0cd014752f4d16124f2b7edd319d6..90b79779e8b9c6e7d0ae6120b36ecf84ebeb84e5`; only an unrelated V25 status claim document changed.
- Not executed: GitHub Actions, repository `.NET` build, Core smoke executable, BricsCAD V25/V26 build/runtime or licensed native qualification. No PASS is claimed for any unexecuted gate.
- No force-push was used.

## Completion

Completed for this bounded MAP-02/MTR-05 hardening lane: claim-first ownership was published, signed-zero representation leakage through public coverage findings was fixed without mutating source state, focused sign-bit regression is committed on `main`, concurrent work was reconciled without overwrite, and unexecuted managed/native gates remain explicitly unclaimed.
