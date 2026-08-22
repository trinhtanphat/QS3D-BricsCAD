# Work claim — Bulk edit target enumeration freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bulk-edit-target-enumeration-freshness-20260812-0841`
- Registered: `2026-08-12T08:41:00+07:00`
- Completed: `2026-08-12T08:44:00+07:00`
- Baseline main SHA: `5b366b0ee39af8fbfed1a05cbd91a10093d7f86d`
- Claim commit: `49d86da0b617dff641693556956212c615e67e46`
- Source fix commit: `cb3ad4068e8bb7639847e6666db2b4068c065499`
- Regression commit: `a96f00e88bf4240001b70c8ba4286bed5e6ab146`
- Priority: P1 evidence-backed Core mutation freshness at a remote-safe boundary

## Completed scope

`BulkEditService` now captures `ProjectState.ChangeVersion` immediately before each caller-controlled target enumeration and fails closed when enumeration changes that version. The guard covers object-target `SetProperty` / `MultiplyNumericProperty` and id-target `SetProperty` / `AssignFamily` paths before any BulkEdit mutation plan is applied or handed to `ProjectSemanticMutationExecutor`.

The guard intentionally does not roll back side effects performed by the caller-owned enumerable itself. It prevents BulkEdit from adding a second semantic mutation on top of a target plan whose project freshness changed during enumeration.

## Implemented surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/BulkEditTargetEnumerationFreshnessSmoke.cs`
- this claim file

## Excluded scope honored

- No changes to `RegenerationEngine` or its separate dirty-subset freshness lane.
- No redesign of `ProjectSemanticMutationExecutor`, rollback snapshots, target bounds, ownership, property canonicality, numeric no-op, Family inheritance/dirty semantics, selection UI, persistence or BricsCAD adapters.
- No GitHub Actions dispatch and no local BricsCAD qualification.

## Validation actually performed

- Re-read the integrated `BulkEditService` from current `main` after source commit `cb3ad4068e8bb7639847e6666db2b4068c065499`; all four public enumerable entry paths capture and compare project version around target enumeration.
- Re-read `BulkEditTargetEnumerationFreshnessSmoke` from current `main` after regression commit `a96f00e88bf4240001b70c8ba4286bed5e6ab146`.
- The smoke covers lazy object-target SetProperty, numeric multiplication, lazy id-target SetProperty and Family assignment; each caller enumerable advances the project exactly once and the assertions require BulkEdit to leave property/Family/dirty state unchanged. A side-effect-free target path still requires the normal mutation and one project version advance.
- Verified `a96f00e88bf4240001b70c8ba4286bed5e6ab146` remains an ancestor of current `main` snapshot `92fe422a809309bd818fb6be68baa90dfd1f53cd` (`behind_by: 0`).
- No local .NET build or smoke execution is claimed from this connector-only environment.
- No BricsCAD V25/V26 runtime PASS is claimed.
- No GitHub Actions were dispatched and no force-push was used.

## Coordination

This completed reservation remained disjoint from the ACTIVE Regeneration dirty-subset freshness lane. Previously completed BulkEdit numeric no-op, empty-property, family canonicality/dirty and target-bound behavior was not redesigned.

## Completion condition

Completed. Current `main` fails closed when BulkEdit caller target enumeration changes project version, focused regression source is committed, and the exact integration SHAs are recorded above.