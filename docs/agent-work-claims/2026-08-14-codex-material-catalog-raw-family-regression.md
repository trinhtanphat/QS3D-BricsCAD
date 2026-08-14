# Work claim — Restore Material Catalog raw FamilyId regression boundary

- Status: `COMPLETED`
- Agent: `codex-material-catalog-raw-family-regression-20260814` (`/root/fix_level_curtain_frame_z`)
- Registered: `2026-08-14T16:01:00+07:00`
- Completed: `2026-08-14T16:04:00+07:00`
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

- Successor claim PR `#1250` merged as `d4178ff7670f0d6f3b4520c843165c85451ab208`.
- Test commit `13192874cfcacf254dcdba182672b49db83d39c0` merged through PR `#1253` as `f2c9955cacadaee9989a6586b212c5b7b83de27e`.
- The smoke now constructs and asserts canonical `FamilyId`, injects only private `_familyId` with the padded raw boundary through test-local reflection, asserts the injection, and retains the original #525 Family material rename, inherited generated-solid stale, Properties/Quantity dirty, and final exact raw no-rewrite assertions.
- Core Release build PASS with `0 warnings / 0 errors`. Material Catalog dark-selection, integrity, open-project lifecycle, project lifecycle, responsive-footer and material/floor picker gates PASS unchanged.
- `preflight-material-catalog-atomicity.py` remains independently blocked on the unchanged stale `ProjectStateSnapshot` literal `target.Metadata.Clear();`; neither that gate nor snapshot production is in this claim.
- Full Core smoke advances beyond Material Catalog and stops at the next independent blocker: `ProjectSemanticMutationExecutorSmoke.MutableRelationWhitespaceRollsBackExactly` line 99 expects padded `FamilyId` text although the authoritative relation setter stored `FAM-1`.
- No production, focused gate, LOCAL runner/probe, BricsCAD/native/private data, GitHub Actions, release or packaging surface changed.
