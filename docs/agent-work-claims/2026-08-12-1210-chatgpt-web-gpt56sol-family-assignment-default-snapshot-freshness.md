# Work claim — Family assignment default snapshot freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-assignment-default-snapshot-freshness`
- Registered: `2026-08-12T12:10:00+07:00`
- Baseline main SHA: `3291545dff3284d520c642bcb203f22fd952a851`
- Priority: P1 semantic assignment consistency
- Task Key: `CORE-FAMILY-ASSIGNMENT-DEFAULT-SNAPSHOT-FRESHNESS`

## Confirmed defect

`ProjectFamilyService.Assign(...)` validates and snapshots target Family default properties before enumerating the caller-owned target sequence. `ProjectFamily.Properties` is a publicly mutable dictionary and direct changes do not increment `ProjectState.ChangeVersion`. A lazy target enumerable can therefore yield a valid target and then directly change a target Family default such as `Material` from `Steel` to `Concrete`. Existing revision/global-identity/ownership checks all pass, but assignment later applies the stale pre-enumeration `Steel` snapshot to the element while the element now points at a Family whose current default is `Concrete`.

The existing Family-default integrity contract requires target defaults to be validated before instance mutation; retaining the initial preflight while refreshing the snapshot after target enumeration preserves that contract and prevents stale inheritance.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — target default snapshot refresh after enumeration/ownership validation only
- `tests/QS3D.Core.SmokeTests/FamilyAssignStructuralFreshnessSmoke.cs` — focused target-default freshness regression extension only
- this claim file for close-out

## Intended contract

- Preserve the initial target-default validation before caller target enumeration so malformed-at-entry defaults still fail early.
- After enumeration freshness and current ownership/category validation, validate/snapshot the target Family defaults again and use that current snapshot for assignment inheritance.
- A valid direct target-default change during lazy enumeration must not produce an element whose inherited defaults disagree with its newly assigned current Family.
- A target default changed to malformed state during enumeration must fail through the existing `SnapshotProperties(...)` validator before assignment mutation.
- Preserve Family identity/global freshness, previous-Family cleanup, overrides, no-op behavior and all unrelated Family operations.

## Excluded scope

- No Family property-service redesign, no ProjectState collection/event redesign, no Zone/Floor/Grid changes, no CAD/UI/runtime work, and no concurrent Recognition/Selection/Curtain/Auto Room/Interchange work.
- No force-push, GitHub Actions dispatch, full-build/executable-smoke PASS, or licensed BricsCAD V25/V26 runtime qualification claim.

## Validation plan

Re-fetch exact source/smoke after claim registration. Keep the existing early snapshot validation, refresh the target default snapshot only after post-enumeration ownership checks, extend the existing auto-registered smoke with a lazy direct `Material` change and malformed-default case, read back integration, close with exact SHAs, and verify completion ancestry.