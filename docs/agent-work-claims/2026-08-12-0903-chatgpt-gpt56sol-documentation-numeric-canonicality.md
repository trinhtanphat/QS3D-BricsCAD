# Work claim — Documentation Catalog numeric lexical canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-documentation-numeric-canonicality`
- Registered: `2026-08-12T09:03:00+07:00`
- Baseline main SHA: `4601218af86e01f1909cc7bf688bc87315e59e88`
- Priority: deterministic persisted-format integrity during owner-requested continue-all audit
- Task Key: `CORE-DOCUMENTATION-CATALOG-NUMERIC-CANONICALITY`

## Confirmed defect

`SemanticDocumentationCatalogStore.Serialize(...)` emits persisted sheet dimensions and placement coordinates/sizes with invariant round-trip (`"R"`) numeric formatting, while `Load(...)` currently accepts any finite `double` lexical spelling. Semantically equivalent persisted tokens such as `1000.0` are therefore accepted and silently rewritten as `1000` on the next save. This violates the catalog's existing strict lossless/canonical persisted-format boundary.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- one focused Core smoke file for numeric lexical canonicality
- this claim file for close-out

## Contract

- every persisted Documentation Catalog numeric attribute parsed by the shared numeric helper must already equal the exact invariant round-trip representation emitted by `Serialize(...)`;
- reject finite but noncanonical numeric lexical spellings before catalog materialization;
- preserve existing finite/range/schema/count validation and all semantic documentation behavior;
- do not broaden into collection-order canonicality, UI/native BricsCAD, release/update, or unrelated documentation surfaces.

## Validation plan

Add deterministic Core smoke coverage proving a canonical saved catalog still loads, while semantically equivalent noncanonical sheet/placement numeric tokens fail closed instead of being normalized on a later save.

No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this remote lane.
