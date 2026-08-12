# Semantic Schedule CDATA grammar

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: ACTIVE
- Registered: 2026-08-12 14:06 +07:00
- Baseline main: `c8dfe4cadae7a280e4adfd668b78be6e26c76848`

## Defect

`SemanticScheduleCatalog.ValidateElement(...)` accepts whitespace-only `XCData` because LINQ-to-XML `XCData` derives from `XText` and the validator currently allows whitespace `XText`. The writer never emits CDATA, and the sibling `SemanticDocumentationCatalogStore` explicitly rejects CDATA, so malformed/non-writer-owned Schedule XML can pass the strict schema boundary.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs` — explicit CDATA rejection in `ValidateElement(...)` only.
- `tests/QS3D.Core.SmokeTests/SemanticScheduleCDataGrammarSmoke.cs` — focused regression.
- This claim file.

## Intended contract

- Any CDATA node in the Semantic Schedule catalog fails closed before definition materialization.
- Ordinary whitespace text between XML elements remains accepted.
- Existing element/attribute/schema/category/canonical text behavior is unchanged.

## Validation boundary

Remote source-level regression/readback only in this lane. No GitHub Actions, full .NET build, Core executable smoke, or licensed BricsCAD V25/V26 runtime PASS is claimed unless separately run and recorded.
