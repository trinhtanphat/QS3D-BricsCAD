# Work claim — Family assignment default snapshot freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-assignment-default-snapshot-freshness`
- Registered: `2026-08-12T12:10:00+07:00`
- Baseline main SHA: `3291545dff3284d520c642bcb203f22fd952a851`
- Priority: P1 semantic assignment consistency
- Task Key: `CORE-FAMILY-ASSIGNMENT-DEFAULT-SNAPSHOT-FRESHNESS`

## Confirmed defect

`ProjectFamilyService.Assign(...)` validated and snapshotted target Family default properties before enumerating the caller-owned target sequence. `ProjectFamily.Properties` is a publicly mutable dictionary and direct changes do not increment `ProjectState.ChangeVersion`. A lazy target enumerable could therefore yield a valid target and then directly change a target Family default such as `Material` from `Steel` to `Concrete`. Existing revision/global-identity/ownership checks passed, but assignment later applied the stale pre-enumeration `Steel` snapshot to the element while the element pointed at a Family whose current default was `Concrete`.

The existing Family-default integrity contract requires target defaults to be validated before instance mutation; retaining the initial preflight while refreshing the snapshot after target enumeration preserves that contract and prevents stale inheritance.

## Completed implementation

- Preserved the initial `SnapshotProperties(target, "Target", "assignment")` preflight before caller enumeration, so malformed-at-entry defaults still fail early.
- After target enumeration freshness and current global/target ownership/category validation, `Assign(...)` now validates and snapshots the current target Family defaults again.
- Assignment inheritance uses that refreshed snapshot, preventing current Family defaults from diverging from newly assigned inherited instance defaults.
- A target Family changed to malformed property state during lazy enumeration fails through the existing canonical `SnapshotProperties(...)` validator before `project.Touch()` or assignment mutation.
- Existing Family identity/global freshness, previous-Family cleanup, overrides and no-op behavior remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/FamilyAssignStructuralFreshnessSmoke.cs` remains auto-registered with `ModuleInitializer` and adds two focused cases:

1. `Material` changes directly from `Steel` to `Concrete` during lazy target enumeration; the assigned element must inherit current `Concrete`, not the stale pre-enumeration `Steel` snapshot.
2. A non-canonical target default key is inserted directly during lazy enumeration; assignment must fail before element/revision/dirty/timestamp mutation.

The existing removal and unrelated-duplicate structural-freshness cases remain intact.

## Integration evidence

- Claim commit: `0973e07e062866d273b79505a68cf75820e2e70d`
- Production fix: `34f85714dabc34067e668a8d322adc6a25d470c6` (`fix(family): refresh assignment defaults after enumeration`)
- Focused regression: `0fb3e8b7878131525fdfa1d973441055c20e61db` (`test(family): guard assignment default snapshot freshness`)
- Integrated source blob read-back: `a087e1f47cfbce85dccb46738dff0fca09bd4792`; it retains the initial preflight and refreshes `targetProperties` immediately after post-enumeration ownership validation.
- Integrated smoke blob read-back: `a0b65c87600b2ff3c9672a5ec160499af2ba8a68`; it covers current-default inheritance and malformed-during-enumeration rejection in addition to prior structural cases.

## Excluded scope / validation boundary

- No Family property-service redesign, no ProjectState collection/event redesign, no Zone/Floor/Grid changes, no CAD/UI/runtime work, and no concurrent Recognition/Selection/Curtain/Auto Room/Interchange changes.
- No force-push and no GitHub Actions dispatch.
- No full-build/executable-smoke PASS or licensed BricsCAD V25/V26 runtime qualification is claimed from this connector-only lane; validation is repository integration/read-back plus focused regression source coverage.