# Work claim — Bulk Family previous-category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-family-previous-category-integrity`
- Registered: `2026-08-12T10:48:00+07:00`
- Baseline main SHA: `a23b0fc19e6445d8f5d00e88cc4bac7329170860`
- Priority: P1 — fail closed on corrupted previous Family/category relations before bulk reassignment mutates inherited properties.

## Confirmed defect

`ProjectFamilyService.Assign()` rejects an element whose existing Family resolves to a different `ElementCategory`, but `BulkEditService.AssignFamily()` currently snapshots that mismatched previous Family and uses its properties to determine inherited-key cleanup before assigning the new Family. A persisted/mutated corrupted Family relation can therefore be silently masked by bulk reassignment and can remove instance properties based on a Family from the wrong category.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`, limited to previous-Family/category validation inside `AssignFamily()`
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Before any mutation, resolve every non-empty previous Family reference for pending targets.
- Reject when the previous Family category differs from the element category, matching the single-element `ProjectFamilyService.Assign()` contract.
- Preserve missing-Family rejection, target-Family category checks, property inheritance/override behavior, all-or-nothing mutation, ownership/freshness guards, dirty flags, and canonical no-op behavior.
- Regression must prove the malformed target is rejected without changing project version, FamilyId, or element properties.

## Excluded scope

- General bulk target structural-freshness changes.
- Family identity/property canonicality changes unrelated to category mismatch.
- UI, BricsCAD runtime, exporter, persistence, or GitHub Actions changes.
