# Work claim — release #27 family reassignment generic preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:19:00+07:00`
- Completed: `2026-08-12T08:24:00+07:00`
- Baseline main SHA: `b93aaf08119d53a5316b39864816871c6704b6fa`
- Trigger: QS3D Cloud V25 Preview Build & Release #27 (`31553002883`) failed in `scripts/preflight.py` with `family reassignment must refresh inherited defaults without overwriting instance overrides`.

## Reserved scope

Reconcile the generic family-reassignment source guard with the current validated `BulkEditService.AssignFamily` implementation. Preserve the behavior requirement that inherited defaults refresh on reassignment while instance overrides remain untouched, and preserve canonical validation of both target and previous Family property snapshots.

## Completed implementation

Implementation commit: `a2e3bf61f248e26f3654ac344666d6469c6254df` (`fix(preflight): match canonical family default snapshots`).

The generic guard no longer requires the obsolete direct `previousFamily.Properties` token. It now requires the current fail-closed reassignment contract:

- `inheritedKeys`
- `ProjectFamilyService.SnapshotProperties(family, "Target", "bulk assignment")`
- `ProjectFamilyService.SnapshotProperties(previousFamily, "Previous", "bulk assignment")`
- `targetPropertyKeys`
- `previousProperties`

No `BulkEditService.AssignFamily` production code was changed. The existing implementation already validates target/previous Family defaults through canonical snapshots before deriving inherited keys and applying target defaults.

## Regression evidence

`ContinuationRegressionSmoke.FamilyAssignmentRefreshesInheritedDefaults()` remains the behavioral regression authority. It verifies inherited `ThicknessM` refresh, new `HeightM` inheritance, `Material` instance override preservation, unrelated instance property preservation, old inherited-key removal, dirty propagation, and canonical same-family no-op behavior.

## Validation boundary

- Direct readback of `scripts/preflight.py` on `main` confirms the generic guard now requires the canonical snapshot/inherited-key tokens.
- The release #27 failure was a stale generic source assertion exposed after `BulkEditService` hardening; it was not evidence that family reassignment behavior had regressed.
- The MSI URL `throw` text shown earlier in run #27 belongs to the successfully completed cloud prerelease validation script body; the job stopped later at `scripts/preflight.py`.
- Run #27 remains tied to its original SHA and must not be rerun as proof of this newer `main` commit.
- No licensed BricsCAD V25 runtime, signing, installer, package or clean-machine PASS is claimed from this remote lane.
