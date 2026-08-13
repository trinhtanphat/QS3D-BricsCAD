# Work claim — MAP-02 coverage control-character identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map02-control-identity-20260813-1822`
- Registered UTC: `2026-08-13T11:22:00Z`
- Last updated UTC: `2026-08-13T11:24:00Z`
- Baseline main SHA: `f914469f95706bec4561bf93271017c94653a558`
- Priority: `MAP-02 / P0-P1 hardening` — fail closed on non-canonical element identity before quantity/work-item coverage projection

## Confirmed defect

`MeasurementWorkItemCoverageEvaluator` snapshotted project element identities through `RequireCanonicalIdentity()`, but that helper rejected only blank text and leading/trailing whitespace. `ProjectElement` can exist in memory with an ID containing a control character, so the coverage evaluator could return a seemingly valid finding for a non-canonical identity. This was inconsistent with MAP-01 mapping identifiers and MeasurementTrace canonical text, both of which reject control characters, and with QSDB XML publication constraints.

The completed MAP-02A claim promised fail-closed handling for non-canonical element identity; its existing corruption smoke covered duplicate/null/non-finite/padded/undefined-category state but not control-character element IDs.

## Reserved files

- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`
- this claim file

## Implemented scope

- Extended the existing `RequireCanonicalIdentity()` helper to reject any `char.IsControl(...)` character with `InvalidOperationException`.
- Because this helper is shared by element IDs and quantity keys, both canonical identity uses now receive the same control-character fail-closed guard without changing their lookup/order semantics.
- Added a focused corruption regression using an element ID `Bad\u0001Id`; coverage must throw before returning findings.
- Preserved mapped/unmapped/stale/missing semantics, deterministic ordering, mapping resolution and quantity values for valid identities.
- Did not change `ProjectElement`, MAP-01 mapping catalog, ProjectState, QSDB/schema/migration, MeasurementTrace/Snapshot/Delta/REV-03, reports/UI, rates/cost, geometry or BricsCAD/native surfaces.

## Coordination / overlap reconciliation

- Claim-only commit: `81d1fe7ca110e49497bcd3daa2c13c342b602830` — `chore(agent): claim MAP-02 control-character identity guard`.
- Post-claim refresh showed the claim at HEAD with no intervening reserved-file change before source write.
- Concurrent Quantity Summary UI and NETLOAD/runtime work remained outside Mapping source/test scope.
- After the regression commit, `main` advanced to `945f042b66d9da4882cd6f255ecedb1ad6789916`; GitHub compare from `8c1d96e0915a31b34130c954c056b73b53f2dbba` showed one new Platform/CAD claim document only, with no reserved MAP file changes.
- REV-03A remained independently `ACTIVE`; no Revision/Measurement file was touched by this lane.

## Implementation commits

- `40c012195d44b81bcd777eb2c917aa058a1c3041` — `fix(mapping): reject control-character coverage identities`.
- `8c1d96e0915a31b34130c954c056b73b53f2dbba` — `test(mapping): guard control-character coverage identity`.

## Validation actually executed

- Executed: current-`main` refresh before claim, post-claim ownership recheck, post-implementation refresh and final reconciliation.
- Executed: exact GitHub commit diff inspection for source and regression commits; each changed only its single reserved implementation/test file.
- Executed: direct current-`main` readback confirmed the control-character guard and focused `Bad\u0001Id` regression remain present.
- Executed: GitHub compare `8c1d96e0915a31b34130c954c056b73b53f2dbba..945f042b66d9da4882cd6f255ecedb1ad6789916`; only an unrelated Platform/CAD claim document changed.
- Not executed: GitHub Actions, repository `.NET` build, Core smoke executable, BricsCAD V25/V26 build/runtime or licensed native qualification. No PASS is claimed for any unexecuted gate.
- No force-push was used.

## Completion

Completed for this bounded MAP-02 hardening lane: claim-first ownership was published, the current-source canonicality gap was proven, control-character identities now fail closed in the coverage snapshot helper, focused regression source is committed on `main`, concurrent work was reconciled without overwrite, remote readback confirmed the landed code, and unexecuted managed/native gates remain explicitly unclaimed.
