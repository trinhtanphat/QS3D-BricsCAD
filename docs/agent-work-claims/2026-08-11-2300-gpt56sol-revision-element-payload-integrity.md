# Work claim — Revision element payload canonical integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revision-payload-20260811-2300`
- Registered: `2026-08-11T23:00:00+07:00`
- Completed: `2026-08-11T23:04:00+07:00`
- Baseline main SHA: `0c32164f96fdc7d9fa4d7abc9dd855fcc6c49826`
- Claim commit: `b143614d07b87ecaff1c35e8456e9478f73e6637`
- Revision compare/capture fix commit: `8d2d27c2cdc37811a1cc3fd41444446bf933f648`
- Quantity report fix commit: `07877d41200e2ffd7b35f5fa2d0b7f428f782986`
- Regression commit: `4564b0b8014901ccbdfae2631edd318ced4394d3`
- Priority: P2 source-proven regression hardening

## Reserved scope

Finish the source-safe Revision element invariant alignment between public/in-memory snapshots and `RevisionSnapshotStore.Save`. The persistence boundary requires canonical category text, canonical property/quantity keys, finite quantities, and canonical unique source/dependency lists. `RevisionService.Capture` could copy non-canonical map keys from directly mutable `ProjectElement` dictionaries, while `RevisionService.Compare` could bypass payload validation entirely for Added/Removed elements and normalize malformed dependency lists during field comparison. `QuantityRevisionReport.Build` also accepted non-canonical quantity keys when equal values produced no rows.

## Implemented surfaces

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file

## Implemented fix

- Capture now rejects non-canonical property and quantity keys before emitting a revision element snapshot.
- Compare indexing now validates canonical category names, property/quantity keys, finite quantity values, canonical source handles, and canonical unique dependency values before Added/Removed/Changed classification.
- Existing canonical Family/Floor/Zone reference checks and case-insensitive element identity behavior are preserved.
- Quantity revision indexing now validates canonical category, canonical quantity keys and finite values before building rows, including equal malformed values that previously produced no row and no error.
- Regression coverage exercises direct mutable project map keys, malformed Added/Removed snapshot category/map/value/source/dependency payloads, and equal padded quantity keys in the quantity report.

## Explicit exclusions honored

- No revision XML schema/version or `RevisionSnapshotStore` changes.
- No Revision UI/code-behind or BricsCAD coordinator changes.
- No changes to `ProjectElement` dictionary/list ownership architecture.
- No quantity calculation/rule semantics changes.
- No GitHub Actions dispatch or workflow edits.

## Validation actually performed

- Verified the claim commit was current `main` before substantive writes and re-fetched exact source/test blobs.
- Used current blob SHA checks for all implementation writes; no force push/reset was used.
- Re-fetched current `main` after implementation and verified capture map-key guards, compare pre-classification payload validation, quantity-report index validation, and all new regression methods in the already-registered `RevisionRegressionSmoke.Run()` suite.
- Cross-checked the enforced rules against the existing `RevisionSnapshotStore.ValidateSnapshot` canonical category/map/list/finite-value contract.
- No local checkout/.NET build/Core smoke execution was available in this connector-only lane; executable PASS is not claimed.
- No BricsCAD V25 runtime or GitHub Actions execution is claimed.

## Coordination

The preceding Revision Core claims were completed before this batch. Recent Revision UI/read-only claims are completed and disjoint, so this implementation remained inside Core Revision payload validation only.

## Completion condition

Completed. Revision capture/compare/report no longer silently accepts the covered element payload forms that revision persistence rejects, focused regression coverage is committed on current `main`, current source was re-read, and this claim records exact SHAs and the actual validation boundary.
