# Work claim — Semantic Documentation catalog XML current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-documentation-catalog-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `5ddd99af06bd209c562cd2c60cb22563ef565528`
- Issue: `#1500`
- Superseded integration-v2 PR: `#1502`
- Branch: `agent/chatgpt-gpt56sol/documentation-catalog-xml-main-recovery-20260815`
- Priority: Core P1 persisted documentation integrity

## Confirmed current-main defect

`SemanticDocumentationCatalogStore.CanonicalRequiredText(...)` and `CanonicalOptionalText(...)` still accept XML-illegal UTF-16 on current main and only fail later during XML materialization. The completed integration-v2 lane #1500/#1502 already established the narrow correct contract: XML representability must be checked at the centralized persisted-text boundary before project mutation/persistence publication.

## Reserved recovery surfaces

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogXmlPersistabilitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogXmlPersistabilityRegistration.cs`
- this claim file

## Recovery constraints

- rebuild from exact current main; do not retarget or force-push the stale integration-v2 branch;
- preserve all current-main Documentation/View bounds and planner behavior;
- no Semantic Schedule, ProjectState, BCF/IFC, native documentation runtime, schema, workflow/release or product-boundary changes;
- no direct main merge and no manual GitHub Actions dispatch/rerun;
- managed/native PASS only when actually executed in an appropriate environment.

## Prior reviewed evidence

- original v2 source: `6814c81b4067595f34523446662b6899aabebb85`
- original v2 smoke: `6693460abb4036741894de33e6e1f08b1bcd2959`
- original v2 registration: `60c20f46d0817325ccaadd3a02bfd07689506ef7`

Implementation begins only after this claim is published on the dedicated recovery branch and Issue #1500 is visibly ACTIVE.
