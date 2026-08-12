# Work claim — Project Browser workspace XML canonical content

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-browser-workspace-xml-canonicality`
- Registered: `2026-08-12T13:20:00+07:00`
- Baseline main SHA: `d9e3f56e5f29fbee38f943bab967a21470d20d29`
- Claim commit: `9ef813f82a68dcd9d38e5c0a77081c716afd3c76`
- Source fix: `699c26dee7897111866ecbabd63dc44e8e0f4d99`
- Regression smoke: `34637af83161a538d9cb2af81ea5a86ac6f41022`
- Smoke registration: `a3bcfe9c39e44ace69b9183b68ef27b4a58b243d`
- Priority: P1 — non-canonical XML nodes could load successfully and then be silently rewritten or discarded on persistence.

## Confirmed defect

`ProjectBrowserWorkspaceStateStore.Deserialize()` validated the root subtree but not unsupported `XNode` siblings at the `XDocument` level. In addition, both `ValidateItemShape()` and `ValidateContainerNodes()` accepted `XCData` through their `XText` checks because LINQ to XML `XCData` derives from `XText`.

Concrete counterexamples were document-level comments/processing instructions and CDATA. They could pass the prior shape checks even though `Serialize()` never emits those representations, so a load/save cycle could silently canonicalize or remove input representation rather than rejecting it.

## Completed change

- `Deserialize()` now validates the `XDocument` node set before root-shape/schema parsing and rejects every sibling node other than the root element.
- `ValidateItemShape()` rejects `XCData` before generic `XText` acceptance.
- `ValidateContainerNodes()` rejects `XCData` before its whitespace-text allowance.
- XML declarations remain accepted because they are not members of `XDocument.Nodes()`.
- Existing namespace/schema/order checks, semantic filters, selection behavior, and serialization format are unchanged.

## Regression coverage

`ProjectBrowserWorkspaceXmlCanonicalitySmoke` covers:

- canonical serialized XML still deserializes and preserves a floor filter;
- an XML declaration still deserializes successfully;
- CDATA inside an item is rejected;
- whitespace-only CDATA at root/container level is rejected;
- a document-level comment is rejected;
- a document-level processing instruction is rejected.

A module initializer registers the smoke with the existing Core smoke-test assembly.

## Readback verification

Read back from `main` at `4c1a9cb3bef421f9dee3a7ea52ad9dbe3e9e9bf5`: source still contains document-node validation before `ValidateRootShape`, both CDATA guards remain ahead of their `XText` paths, and both smoke files remain present.

## Validation boundary

No GitHub Actions, local build/smoke execution, or BricsCAD runtime PASS is claimed. Verification in this lane is source/regression registration plus post-write `main` readback only.