# Work claim — release #27 family reassignment generic preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:19:00+07:00`
- Baseline main SHA: `b93aaf08119d53a5316b39864816871c6704b6fa`
- Trigger: QS3D Cloud V25 Preview Build & Release #27 (`31553002883`) failed in `scripts/preflight.py` with `family reassignment must refresh inherited defaults without overwriting instance overrides`.

## Reserved scope

Reconcile the generic family-reassignment source guard with the current validated `BulkEditService.AssignFamily` implementation. Preserve the behavior requirement that inherited defaults refresh on reassignment while instance overrides remain untouched, and preserve canonical validation of both target and previous Family property snapshots.

## Expected surfaces

- `scripts/preflight.py`
- this claim file

## Exclusions

- no change to `BulkEditService.AssignFamily` unless a source defect is independently proven
- no weakening/removal of generic or feature preflight blockers
- no unrelated Family/Room/UI work
- no BricsCAD runtime/release PASS claim

## Evidence / validation target

- Current `AssignFamily` uses `ProjectFamilyService.SnapshotProperties(family, "Target", "bulk assignment")` and `ProjectFamilyService.SnapshotProperties(previousFamily, "Previous", "bulk assignment")`, then derives `inheritedKeys` from the validated previous snapshot before applying target defaults.
- `ContinuationRegressionSmoke.FamilyAssignmentRefreshesInheritedDefaults()` directly verifies inherited `ThicknessM` refresh, new `HeightM` inheritance, `Material` instance override preservation, unrelated instance property preservation, and removed old-default cleanup.
- Update the generic guard to require the current canonical snapshot/inherited-key contract instead of the obsolete direct `previousFamily.Properties` token.
