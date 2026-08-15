# Work claim — Semantic Documentation catalog XML persistability

- Status: `ACTIVE` — implementation complete, ready for authorized integration
- Agent: `chatgpt-gpt56sol-documentation-catalog-xml-20260815`
- Registered: `2026-08-15T09:21:37+07:00`
- Current main SHA at latest status refresh: `3e5bb15b6f55968234bcc2c784c07e770f88439a`
- Integration-v2 baseline/latest observed SHA: `df6c50276e74684a9041305bd3db6a966d327105`
- Issue: `#1500`
- PR: `#1502` (ready for review)
- Branch: `agent/chatgpt-gpt56sol/documentation-catalog-xml-persistability-20260815`
- Priority: Core P1 persisted documentation integrity

## Confirmed defect

`SemanticDocumentationCatalogStore.Save(...)` validates semantic view/sheet definitions before serializing them into the project metadata XML payload. Every serialized textual attribute passes through `CanonicalRequiredText(...)` / `CanonicalOptionalText(...)`, but those helpers enforced only blank/trim behavior. XML-illegal UTF-16 could therefore survive the canonical planning layer and fail only during `XAttribute` construction in `Serialize(...)`.

Serialization occurs before `project.Touch()`, so the prior behavior remained state-atomic. This lane closes the canonical persistence/fail-fast contract without changing planning or product behavior.

## Implemented fix

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`: required/optional canonical serialized text now routes through one `RequireXmlText(...)` helper using `XmlConvert.VerifyXmlChars(...)`; XML failure becomes `InvalidOperationException` at the store boundary.
- Existing planner/reference/schema/capacity/XML shape is unchanged.
- Focused smoke rejects XML-invalid view/sheet required text and optional title-block text before project revision/timestamp/metadata mutation.
- Valid supplementary Unicode survives Documentation Store Save/Load and QSDB SaveNew/Load exactly.

## Evidence

- claim: `ebd2b154da900c3e7e8d8a968e4e6072643ee0b2`
- source: `6814c81b4067595f34523446662b6899aabebb85`
- smoke: `6693460abb4036741894de33e6e1f08b1bcd2959`
- registration: `60c20f46d0817325ccaadd3a02bfd07689506ef7`
- first handoff claim: `d648408bbd47d6f32f876392cb482273649e3726`
- PR: `#1502`; latest status `open`, `draft=false`, `mergeable=true`
- pre-PR compare: ahead 4, behind 0; exactly four task files
- production source delta: +15/-2, limited to the centralized canonical text helpers
- exact GitHub source/diff readback: PASS
- managed build/smoke: NOT_RUN because this session has no `dotnet`; no LOCAL_PASS claimed
- BricsCAD runtime: NOT_RUN and outside this Core-only lane
- no GitHub Actions manually dispatched/rerun by this session

## Coordination / exclusions

- #1490/#1492 owns the separate Semantic Schedule catalog and is already imported into v2.
- #1503 owns a separate `SemanticViewPlanner` category-input-bound lane; no planner overlap here.
- #77 remains only the broad Documentation umbrella.
- Current main movement after the task baseline is unrelated LOCAL-004/runtime work; this PR targets integration-v2 and is not stale against that target.
- No `SemanticViewPlanner`, `SemanticSheetPlanner`, native documentation runtime, ProjectState, schema version, adapter/native, workflow/release or product-boundary changes.
- No direct integration/main merge by this normal-agent session.

## Handoff

Implementation/regression are fully represented by ready PR #1502 against the owner-authorized integration-v2 branch. No session-only source change remains. Claim stays ACTIVE until coordinator import/release; do not open a second live task from this normal-agent lane before that release.
