# Work claim — Bulk Family structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:24:00+07:00`
- Baseline main SHA: `d322f2f42a89bd12dfe9c7ea75f9b3e1eec63bb8`
- Priority: P1 — bulk Family assignment must not commit against a target Family or target element that stopped being canonical while caller-provided target IDs were enumerated.
- Task Key: `CORE-BULK-FAMILY-STRUCTURAL-FRESHNESS`

## Confirmed defect

`BulkEditService.AssignFamily(...)` validates and resolves the target `ProjectFamily`, snapshots its properties, then materializes caller-provided `elementIds`. The method only compares `ProjectState.ChangeVersion` after enumeration. Because `ProjectState.Families` and `ProjectState.Elements` are public mutable collections, a lazy enumerable can remove/replace the resolved Family or a resolved element without calling `project.Touch()`. The current code can then continue with stale object references and assign a Family that no longer belongs to the canonical project.

`ProjectZoneService.Assign(...)` already closes the equivalent structural-freshness gap by revalidating the target Zone and every resolved element by reference after caller enumeration.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs` — `AssignFamily(...)` structural freshness only
- one focused regression/preflight or Core smoke covering target-Family/element replacement-removal during lazy ID enumeration
- this claim file for close-out

## Intended contract

- preserve existing `ChangeVersion` freshness guard;
- after caller ID enumeration, require the resolved target Family to still be the exact canonical project-owned instance;
- require every resolved target element to still be the exact canonical project-owned instance;
- fail before Family/category/property/dirty/project mutation when structural ownership changed without a version bump;
- preserve canonical no-op behavior, Family property inheritance, global Family identity validation, target bounds, diagnostics and transactional mutation semantics;
- do not edit LOCAL-003 reserved smoke fixtures, semantic-selection code, ProjectFamilyService, template/regeneration/browser lanes or persistence schema.

## Validation boundary

Source and focused regression will be read back from `main`. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
