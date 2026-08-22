# Work claim — Material Catalog padded FamilyId smoke reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T15:53:00+07:00`
- Baseline main SHA: `f9bc896a7ae5fe4fc6a3abb1059409a7119547c3`
- Priority: next deterministic full Core smoke blocker reported by PR #1240

## Confirmed fixture drift

PR #1240 validated the Floor/Zone assignment fix and advanced full Core smoke to `ProjectMaterialCatalogSmoke.RenameStalesInheritedConsumerWithPaddedFamilyId`.

That regression assigns `inherited.FamilyId = "  " + family.Id + "  "` and later expects the padded value to remain. Current `ProjectElement.FamilyId` canonicalizes optional relation identity on assignment, so the stored relation is already the trimmed alias `f-padded` before `ProjectMaterialCatalog.UpsertCustom(...)` runs. The material rename contract should still stale the inherited consumer, dirty Properties/Quantity, and must not perform an additional FamilyId rewrite during the rename itself.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs`
- this claim document only

Reconcile only the padded-FamilyId regression expectation/message with canonical relation storage. Preserve Family material rename, inherited-consumer staleness, dirty flags, override behavior, corrupt-reference atomicity, deletion safety, and all production semantics.

## Explicit exclusions

- no changes to `ProjectElement`, `ProjectMaterialCatalog`, persistence, relation normalization, UI/native BricsCAD, release/workflow, private/local data, #1005/#1106/#982, or other Material/Reporting claims;
- do not absorb the Rebar schedule resource-bound claim or the Floor/Zone claim;
- report the next independent full-smoke blocker rather than expanding scope.

## Validation

- exact diff/readback proving only the stale padded relation expectation/message changed;
- Core Release/full smoke if available through owner validation; otherwise do not claim suite PASS;
- preserve all focused material contract semantics.

## Completion record

- Claim-only PR `#1243` merged at `ad86ce7999f234b46925d0c8bc5b2916a44f7465` before implementation.
- Implementation commit `64f3d0aef2acd85d05829e5043c0631d9446dbab` changed only the final padded-FamilyId assertion/message; branch compare showed exactly one addition and one deletion in `ProjectMaterialCatalogSmoke.cs`.
- Implementation PR `#1248` merged to `main` at `22bab55d7c3b9eda453ba8ea9c358d2a23576d1f`.
- Readback of merged `main` confirms the regression still assigns a padded FamilyId, preserves the material rename/stale/dirty assertions, and now requires the canonical stored `family.Id` after rename.
- No production code, preflight gate, native/local runner, workflow, Actions dispatch, release, or packaging surface changed.
- This tool environment has no `dotnet`, so no fresh Core build/full-smoke PASS is claimed here. The next exact-main runner should use this merged SHA or a descendant and report the next independent blocker if one remains.
