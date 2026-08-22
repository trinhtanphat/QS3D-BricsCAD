# Semantic Schedule CDATA grammar

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: COMPLETED
- Registered: 2026-08-12 14:06 +07:00
- Baseline main: `c8dfe4cadae7a280e4adfd668b78be6e26c76848`
- Claim commit: `6450b35f25ecc268c2ed09ec9e3707f4092bc1b7`
- Source fix: `dd1aff88224a592bb3ad7babe01f9e35781fdf5f`
- Regression smoke: `2d31c0b66e138c6b1379e7a7bac4d59ffcd74996`

## Defect

`SemanticScheduleCatalog.ValidateElement(...)` accepted whitespace-only `XCData` because LINQ-to-XML `XCData` derives from `XText` and the validator allowed whitespace `XText`. The writer never emits CDATA, and the sibling `SemanticDocumentationCatalogStore` explicitly rejects CDATA, so malformed/non-writer-owned Schedule XML could pass the strict schema boundary.

## Completed change

- `ValidateElement(...)` now rejects `XCData` explicitly before the ordinary whitespace-`XText` allowance.
- Existing element, attribute, namespace, ordinary XML whitespace, category and canonical text behavior is unchanged.
- The common validator applies the same fail-closed CDATA grammar to the root, schedule and all nested schedule containers/elements.

## Regression coverage

`SemanticScheduleCDataGrammarSmoke` uses the production `SemanticScheduleCatalog.Save(...)` output as the canonical payload, then:

- confirms ordinary whitespace text inserted after the root opener still loads one schedule;
- inserts whitespace-only CDATA at the same grammar boundary;
- requires `InvalidDataException` with the `unsupported CDATA content` contract.

The smoke is auto-registered with `[ModuleInitializer]` under the existing SDK compile glob.

## Readback verification

Readback on current `main` after the regression commit confirmed the explicit `XCData` guard remains immediately before the `XText` whitespace allowance, and the regression source remains present with the canonical Save → mutate payload → Load path.

## Validation boundary

Remote source/test readback only in this lane. No GitHub Actions, full .NET build, Core executable smoke, or licensed BricsCAD V25/V26 runtime PASS is claimed.
