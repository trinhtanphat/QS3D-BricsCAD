# Work claim — Material rename inherited FamilyId canonicalization

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:17:00+07:00`
- Completed: `2026-08-11T23:23:00+07:00`
- Baseline main SHA: `df0df09f65cb9e1da1f20d749984dea4111a548c`
- Claim commit: `4edc480c8e8ad539643eeef33db3c06e23bb95b0`
- Source branch commit: `3abf841916e6231cf508eb01d2e398d05784781c`
- Regression branch commit: `dcb193d689b02877284596c10dd0d51cdf9e6dff`
- PR: `#525`
- Merged main SHA: `7d27c213d7eac7bae4b333398efbcbc21d57faec`
- Priority: evidence-driven remote-safe Core stale-state hardening

## Reason

`ProjectMaterialCatalog.RenameReferences()` built inherited-material Family sets with canonical Family IDs but checked each element using raw mutable `element.FamilyId`. `ProjectElement.FamilyId` can contain surrounding whitespace after construction, so a semantic reference such as `" f-wall "` could fail to match Family `"f-wall"`. The Family material reference could be renamed while the inheriting element was not marked dirty for Properties/Quantity, leaving generated/material-dependent state stale.

## Implemented scope

Canonicalized the element Family reference only for inherited material/frame-material membership checks during custom material rename. Stored `FamilyId`, true instance overrides, case-insensitive identity semantics, catalog persistence format, rename atomicity, and existing material reference behavior remain unchanged.

## Implemented surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs`
- this claim file

## Regression coverage

`RenameStalesInheritedConsumerWithPaddedFamilyId()` mutates an element `FamilyId` to a padded but semantically identical reference after construction, clears initial dirty/stale state, then renames a Family-inherited custom material. It verifies:

- the Family material reference is renamed;
- the inherited consumer becomes generated-solid stale;
- Properties and Quantity dirty flags are set;
- the stored padded `FamilyId` is not rewritten.

## Explicit exclusions honored

- No Material Catalog WPF/modeless lifecycle changes.
- No Quantity Explorer/reporting changes.
- No project schema/persistence-format change.
- No BricsCAD V25/native runtime change.
- No GitHub Actions dispatch or workflow edit.

## Validation actually performed

- Re-fetched current source/test blobs before implementation.
- Read back the branch source after write and confirmed lookup-only `(element.FamilyId ?? string.Empty).Trim()` canonicalization.
- Compared the claim base with moving `main`; nine concurrent commits touched separate Start Center, Browser, provenance/target-map, and claim files, with no overlap in the two Material files.
- PR #525 reported mergeable before merge and was squash-merged using expected branch head `dcb193d689b02877284596c10dd0d51cdf9e6dff`.
- Source/static review plus committed CAD-independent smoke coverage only. No local .NET/Core smoke execution, BricsCAD V25 runtime PASS, or GitHub Actions execution is claimed.

## Completion condition

Completed on `main` at `7d27c213d7eac7bae4b333398efbcbc21d57faec`.