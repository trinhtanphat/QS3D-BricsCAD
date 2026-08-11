# Work claim — Material rename inherited FamilyId canonicalization

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:17:00+07:00`
- Baseline main SHA: `df0df09f65cb9e1da1f20d749984dea4111a548c`
- Priority: evidence-driven remote-safe Core stale-state hardening

## Reason

`ProjectMaterialCatalog.RenameReferences()` builds inherited-material Family sets with canonical Family IDs but checks each element using raw mutable `element.FamilyId`. `ProjectElement.FamilyId` can contain surrounding whitespace after construction, so a semantic reference such as `" f-wall "` can fail to match Family `"f-wall"`. The Family material reference is renamed, while the inheriting element is not marked dirty for Properties/Quantity, leaving generated/material-dependent state stale.

## Reserved scope

Canonicalize the element Family reference only for inherited material/frame-material membership checks during custom material rename. Preserve the stored `FamilyId` value, true instance overrides, case-insensitive identity semantics, catalog persistence format, rename atomicity, and existing material reference behavior. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No Material Catalog WPF/modeless lifecycle changes.
- No Quantity Explorer/reporting changes.
- No project schema/persistence-format change.
- No BricsCAD V25/native runtime change.
- No GitHub Actions dispatch or workflow edit.

## Validation plan

- Re-fetch exact source/test blobs from current `main` before implementation.
- Add a regression that mutates an element `FamilyId` to a padded but semantically identical reference after construction, then renames a Family-inherited material.
- Verify the Family reference is renamed and the inherited consumer is marked generated-solid stale while a true instance override remains unaffected.
- Verify implementation trims only for lookup and does not rewrite `element.FamilyId`.
- Re-fetch latest `main` before PR/merge and rebase structurally if concurrent commits moved the branch base.
- Source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

Recent Material Catalog strict UTF-8 work is completed, and current active Project Browser selection work touches separate navigation files. Recent Quantity Explorer Family canonicalization is reporting-only. No active claim found for Core material-rename inherited FamilyId canonicalization.

## Completion condition

Current `main` canonicalizes inherited material Family matching during rename without mutating stored references, focused regression coverage is committed, and this claim is marked `COMPLETED` with exact merged SHA and actual validation scope.