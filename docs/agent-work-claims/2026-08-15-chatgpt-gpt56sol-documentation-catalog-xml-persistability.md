# Work claim — Semantic Documentation catalog XML persistability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-documentation-catalog-xml-20260815`
- Registered: `2026-08-15T09:21:37+07:00`
- Current main SHA: `1eb5a757845ac1e978b3a9dccb33f439f9dfa46f`
- Integration-v2 baseline: `df6c50276e74684a9041305bd3db6a966d327105`
- Issue: `#1500`
- Branch: `agent/chatgpt-gpt56sol/documentation-catalog-xml-persistability-20260815`
- Intended PR target: `integration/20260815-merge-all-v2`
- Priority: Core P1 persisted documentation integrity

## Confirmed defect

`SemanticDocumentationCatalogStore.Save(...)` validates semantic view/sheet definitions before serializing them into the project metadata XML payload. Every serialized textual attribute passes through `CanonicalRequiredText(...)` / `CanonicalOptionalText(...)`, but those helpers currently enforce only blank/trim behavior. XML-illegal UTF-16 can therefore survive the canonical planning layer and fail only during `XAttribute` construction in `Serialize(...)`.

Serialization occurs before `project.Touch()`, so the current operation remains state-atomic. This lane closes the canonical persistence/fail-fast contract without changing planning or product behavior.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`: XML-safe centralized canonical serialized-text helpers only.
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogXmlPersistabilitySmoke.cs`.
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogXmlPersistabilityRegistration.cs`.
- this claim file for handoff.

## Coordination / exclusions

- #1490/#1492 owns the separate Semantic Schedule catalog and is already imported into v2.
- #77 remains only the broad Documentation umbrella.
- Existing documentation planning, reference validation, structural freshness, schema/capacity and XML shape remain unchanged.
- No planner, native documentation runtime, ProjectState, schema version, adapter/native, workflow/release or product-boundary changes.
- No direct integration/main ref mutation; task branch + PR only.
- No manual Actions dispatch/rerun; no managed/native PASS inferred.

## Acceptance

- representative XML-invalid required documentation text fails at the canonical store serialization-text boundary before project revision/timestamp/metadata mutation;
- XML-invalid optional title-block text fails with the same no-mutation guarantee;
- valid supplementary Unicode persists exactly through documentation Store Save/Load and canonical QSDB SaveNew/Load;
- final diff stays limited to the store text contract, smoke/registration and claim.
