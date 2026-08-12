# Work claim — ProjectElement SetQuantity dirty propagation

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-project-element-setquantity-dirty`
- Registered: `2026-08-12T11:46:00+07:00`
- Baseline main SHA: `75df009a1c224cce2a33581aba37ef456ae59711`
- Priority: P1 — direct quantity mutation must participate in semantic regeneration dirty tracking.
- Task Key: `CORE-PROJECT-ELEMENT-SETQUANTITY-DIRTY`

## Confirmed defect

`ProjectElement.SetQuantity()` changes the semantic quantity dictionary and timestamp but does not set `ElementDirtyFlags.Quantity`. After an element has been marked clean, a direct quantity change therefore leaves `Dirty == None`.

`RegenerationEngine.Regenerate()` only enters semantic regeneration for candidates whose dirty mask includes `Properties | Relations | Quantity`; consequently a quantity-only mutation can be skipped entirely by the regeneration pass. `SetProperty()` already marks `Properties | Quantity`, confirming quantity freshness is a first-class dirty contract.

## Non-overlap check

Recent commit/claim searches found no ProjectElement SetQuantity dirty-propagation lane. Current Beam Stirrup health, DependencyGraph integrity, curtain-ratio, and reporting freshness lanes own different source files/contracts.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` — only `SetQuantity()` dirty marking
- focused auto-registered Core smoke coverage for SetQuantity dirty semantics
- this claim file

Do not modify regeneration algorithms, dependency graph behavior, persistence format, generated-output stale lifecycle, CAD/runtime code, or unrelated ProjectElement mutation semantics.

## Intended contract

- A changed finite quantity value sets `ElementDirtyFlags.Quantity`.
- Quantity-only mutation does not introduce unrelated dirty flags or generated-geometry stale state.
- Setting an identical quantity value remains a no-op and preserves dirty/timestamp state.
- Existing validation/canonical key behavior remains unchanged.

## Completion condition

Source marks changed quantities dirty through the existing dirty helper, focused smoke coverage pins changed/no-op behavior, source + smoke are read back from `main`, ancestry is verified, and this claim is closed with exact commit SHAs.