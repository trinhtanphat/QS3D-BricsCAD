# Work claim — Documentation Catalog CDATA node shape

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:32:00+07:00`
- Baseline main SHA observed before registration: `cd5d614a81314be4d53997eeb2c514357af90d75`
- Priority: P1 — persisted documentation XML must not accept noncanonical node types as formatting.
- Task Key: `CORE-DOCUMENTATION-CATALOG-CDATA-NODE-SHAPE`

## Confirmed defect

`SemanticDocumentationCatalogStore.ValidateElement(...)` accepts ordinary whitespace formatting by casting non-element nodes with `node as XText`. In LINQ to XML, `XCData` derives from `XText`, so whitespace-only CDATA such as `<![CDATA[   ]]>` is accepted inside documentation/root/collection nodes even though the canonical serializer never emits CDATA. This permits multiple persisted XML representations for the same semantic catalog and weakens the store's otherwise strict fail-closed schema.

The completed Documentation Catalog text-canonicality lane (`40e6425e136032e2362bc43af6fbc581e8cfa64f`) covers attribute text trimming/canonicalization, not XML node type. Its source fix still leaves this `XCData : XText` node-shape hole reachable.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs`
- `tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogCDataNodeShapeSmoke.cs`
- this claim file for close-out

## Contract

- preserve ordinary whitespace `XText` formatting around canonical elements;
- explicitly reject `XCData` and other unsupported non-element XML node types in Documentation Catalog grammar nodes;
- preserve DTD prohibition, attribute/child schema, text/numeric/enum canonicality, count/size bounds, semantic planner validation, Save behavior and BricsCAD/UI behavior;
- do not broaden into Documentation Catalog identity/text-token semantics already completed by earlier lanes.

## Validation plan

Add focused auto-registered Core smoke coverage proving a canonical metadata catalog with ordinary whitespace formatting still loads while whitespace-only CDATA at root and nested collection boundaries fails closed with `InvalidDataException`. Re-fetch source before write and verify readback/ancestry after concurrent commits. No force-push, GitHub Actions dispatch, executable .NET smoke/build PASS, Python PASS or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.