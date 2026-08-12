# Work claim — Bulk Family previous-category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bulk-family-previous-category-integrity`
- Registered: `2026-08-12T10:48:00+07:00`
- Completed: `2026-08-12T10:59:00+07:00`
- Baseline main SHA: `a23b0fc19e6445d8f5d00e88cc4bac7329170860`
- Implementation SHA: `641b04a3c6f68c610c0d9b5464de5f457f988155`
- PR: `#791`
- Priority: P1 — fail closed on corrupted previous Family/category relations before bulk reassignment mutates inherited properties.

## Confirmed defect

`ProjectFamilyService.Assign()` rejects an element whose existing Family resolves to a different `ElementCategory`, but `BulkEditService.AssignFamily()` previously snapshotted that mismatched previous Family and used its properties to determine inherited-key cleanup before assigning the new Family. A persisted/mutated corrupted Family relation could therefore be silently masked by bulk reassignment and could remove instance properties based on a Family from the wrong category.

## Implemented contract

- Every non-empty previous Family reference is resolved before bulk mutation.
- A previous Family whose category differs from the target element category is rejected before property snapshot/cleanup, matching the single-element `ProjectFamilyService.Assign()` integrity contract.
- Missing-Family rejection, target-Family category checks, property inheritance/override behavior, all-or-nothing mutation, ownership/freshness guards, dirty flags, and canonical no-op behavior remain unchanged.
- `BulkFamilyPreviousCategoryIntegritySmoke` covers malformed previous-category rejection with unchanged project version, element timestamp, FamilyId, properties and dirty flags, plus a valid same-category reassignment path.

## Validation

- Squash-merged PR `#791` to `main` as `641b04a3c6f68c610c0d9b5464de5f457f988155`.
- Commit readback confirms the source guard and focused smoke file are both present in the merged commit.
- GitHub combined status returned no status checks for the commit (`statuses=[]`). No GitHub Actions were dispatched by this lane, and no BricsCAD runtime/build PASS is claimed.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`, limited to previous-Family/category validation inside `AssignFamily()`
- `tests/QS3D.Core.SmokeTests/BulkFamilyPreviousCategoryIntegritySmoke.cs`
- this claim file

## Excluded scope

- General bulk target structural-freshness changes.
- Family identity/property canonicality changes unrelated to category mismatch.
- UI, BricsCAD runtime, exporter, persistence, or GitHub Actions changes.
