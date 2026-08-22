# Work claim — Material Catalog padded FamilyId smoke reconciliation

- Status: `ACTIVE`
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

Pending implementation after this claim is merged to `main`.
