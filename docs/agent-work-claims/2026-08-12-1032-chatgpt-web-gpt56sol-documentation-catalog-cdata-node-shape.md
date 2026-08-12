# Work claim — Documentation Catalog CDATA node shape

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:32:00+07:00`
- Completed: `2026-08-12T10:37:00+07:00`
- Baseline main SHA observed before registration: `cd5d614a81314be4d53997eeb2c514357af90d75`
- Priority: P1 — persisted documentation XML must not accept noncanonical node types as formatting.
- Task Key: `CORE-DOCUMENTATION-CATALOG-CDATA-NODE-SHAPE`

## Confirmed defect

`SemanticDocumentationCatalogStore.ValidateElement(...)` accepted ordinary whitespace formatting by casting non-element nodes with `node as XText`. In LINQ to XML, `XCData` derives from `XText`, so whitespace-only CDATA such as `<![CDATA[   ]]>` was accepted inside documentation/root/collection nodes even though the canonical serializer never emits CDATA. This permitted multiple persisted XML representations for the same semantic catalog and weakened the store's otherwise strict fail-closed schema.

The completed Documentation Catalog text-canonicality lane (`40e6425e136032e2362bc43af6fbc581e8cfa64f`) covers attribute text trimming/canonicalization, not XML node type; this lane closes the remaining node-shape hole without changing that earlier contract.

## Delivered contract

- `ValidateElement(...)` explicitly rejects `XCData` before the ordinary `XText` whitespace-formatting path;
- ordinary whitespace `XText` around canonical elements remains accepted;
- root and nested collection whitespace-only CDATA now fail closed with `InvalidDataException`;
- DTD prohibition, attribute/child schema, text/numeric/enum canonicality, count/size bounds, semantic planner validation, Save behavior and BricsCAD/UI behavior were left unchanged.

## Commits

- Claim: `ab767c6182381c83b54d1a0712bf9b8702952251`
- Source fix: `61a5c9d2990fe650d75f94136965023434fb64ee`
- Focused smoke coverage: `9ddfa6d2d3130dc3714d86f5875624161eba24cd`

## Validation

Readback from `main` confirmed the `XCData` rejection precedes the ordinary `XText` whitespace allowance. The auto-registered smoke source verifies ordinary whitespace formatting still loads an empty catalog and that whitespace-only CDATA at both the root and nested `<views>` boundary is rejected.

At observed `main` SHA `4e49bedf178f560b6fa97a3713a28f1cced3cf8c`, ancestry comparison confirmed source commit `61a5c9d2990fe650d75f94136965023434fb64ee` remains an ancestor (`behind_by: 0`); the six concurrent commits after it did not modify `SemanticDocumentationCatalogStore.cs`, and the focused smoke file was present on `main`.

The smoke source was committed and read back but not executed in this connector session. No force-push, GitHub Actions dispatch, executable .NET smoke/build PASS, Python PASS or licensed BricsCAD V25/V26 runtime qualification is claimed.