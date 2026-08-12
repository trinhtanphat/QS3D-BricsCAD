# Work claim — ProjectElement SetQuantity dirty propagation

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-project-element-setquantity-dirty`
- Registered: `2026-08-12T11:46:00+07:00`
- Baseline main SHA: `75df009a1c224cce2a33581aba37ef456ae59711`
- Priority: P1 — direct quantity mutation must participate in semantic regeneration dirty tracking.
- Task Key: `CORE-PROJECT-ELEMENT-SETQUANTITY-DIRTY`

## Confirmed defect

`ProjectElement.SetQuantity()` changed the semantic quantity dictionary and timestamp but did not set `ElementDirtyFlags.Quantity`. After an element had been marked clean, a direct quantity change therefore left `Dirty == None`.

`RegenerationEngine.Regenerate()` only enters semantic regeneration for candidates whose dirty mask includes `Properties | Relations | Quantity`; consequently a quantity-only mutation could be skipped entirely by the regeneration pass. `SetProperty()` already marks `Properties | Quantity`, confirming quantity freshness is a first-class dirty contract.

## Non-overlap check

Recent commit/claim searches found no ProjectElement SetQuantity dirty-propagation lane. Concurrent Beam Stirrup health, DependencyGraph integrity, curtain, reporting, revision, geometry, and opening lanes owned different source files/contracts.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` — only `SetQuantity()` dirty marking
- focused auto-registered Core smoke coverage for SetQuantity dirty semantics
- this claim file

No regeneration algorithm, dependency graph behavior, persistence format, generated-output stale lifecycle, CAD/runtime code, or unrelated ProjectElement mutation semantics were changed.

## Intended contract

- A changed finite quantity value sets `ElementDirtyFlags.Quantity`.
- Quantity-only mutation does not introduce unrelated dirty flags or generated-geometry stale state.
- Setting an identical quantity value remains a no-op and preserves dirty/timestamp state.
- Existing validation/canonical key behavior remains unchanged.

## Completion record

- Claim commit: `092fc2d888bd91fc2ee63d66eb2589d33e305ffd`
- Source fix: `8f223e9cf979d9ea0c030911f60f7d5de4070fe3` (`fix(core): mark SetQuantity changes dirty`)
- Smoke regression: `d56fcb26b76a9c324b0c974f7ee149110981f2eb` (`test(core): cover SetQuantity dirty propagation`)
- Smoke registration: `43487ffa9d52ba7da358212e054696bdb853b276` (`test(core): register SetQuantity dirty smoke`)
- Readback HEAD before closure: `aeee22ed205215307041cf1001ed3cd17bcf0580`
- Source readback confirms `SetQuantity()` now calls `MarkDirtyCore(ElementDirtyFlags.Quantity, false)` after a changed value.
- Smoke readback covers exact Quantity-only dirty state, no generated-geometry stale side effect, identical-value timestamp/dirty no-op, and regeneration participation.
- Ancestry verified: claim/source/smoke/registration are all ancestors of the readback HEAD with `behind_by = 0`; registration HEAD was six commits behind the readback HEAD and still on the same history.
- Commit status checks for registration HEAD: none reported.
- Workflow runs for registration HEAD: none reported.
- No local build, GitHub Actions PASS, or BricsCAD V25 runtime PASS is claimed.

## Completion condition

Completed: changed quantities are marked dirty through the existing helper without generated-output staleness, focused auto-registered smoke coverage is committed and read back from `main`, ancestry is verified, and this claim records exact commit SHAs.