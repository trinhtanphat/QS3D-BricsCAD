# Work claim — Restore Material Catalog raw FamilyId regression boundary

- Status: `ACTIVE`
- Agent: `codex-material-catalog-raw-family-regression-20260814` (`/root/fix_level_curtain_frame_z`)
- Registered: `2026-08-14T16:01:00+07:00`
- Baseline main SHA: `6aa34b7ac2a9331b1ba00dff9d722f78f2f112cd`
- Priority: restore the original #525 regression coverage weakened by PR #1248

## Confirmed coverage regression

Issue lane #525 added `RenameStalesInheritedConsumerWithPaddedFamilyId()` specifically to prove that Material Catalog rename resolves a padded raw element `FamilyId` canonically while leaving that raw relation text untouched. The authoritative public `ProjectElement.FamilyId` setter now trims valid assignments. PR #1248 reconciled its resulting smoke failure by expecting canonical storage, but the unchanged setter-based setup can no longer exercise the lookup-only trim in `ProjectMaterialCatalog.RenameReferences()` or the original no-rewrite boundary.

The prior claim from PRs #1243/#1249 is `COMPLETED`, there is no open PR or ACTIVE/BLOCKED owner for this exact successor, and this claim does not overlap any production or focused-gate surface.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs`
- this successor claim only

Construct the inherited element through the valid canonical public boundary and assert the canonical relation. Then use test-local reflection only on private `_familyId` to inject the padded legacy/corrupt raw relation and assert that injection reached the element. Retain the Family rename, inherited generated-solid stale state, Properties/Quantity dirty flags, and final exact raw no-rewrite assertion from #525.

## Explicit exclusions

- no changes to `ProjectElement`, `ProjectMaterialCatalog`, persistence, relation normalization, Material UI/native code or focused gates;
- no LOCAL probe/runner, BricsCAD/private data, GitHub Actions, release or packaging work;
- no unrelated Material/Reporting/Rebar claim; report the next independent full-smoke blocker rather than expanding scope.

## Validation

- exact diff/readback proving only the one smoke plus `System.Reflection` boundary support changed;
- Core Release build and full deterministic Core smoke;
- focused Material Catalog integrity, atomicity, lifecycle and picker gates unchanged.

## Completion record

Pending implementation and validation after this successor claim is merged to `main`.
