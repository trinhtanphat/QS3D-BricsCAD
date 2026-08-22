# Work claim — Template Profile XML node canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-profile-xml-node-canonicality`
- Registered: `2026-08-12T13:36:00+07:00`
- Baseline main SHA: `1ccd128f538aa2215a9599af6d30d4bd5f3baa3c`
- Claim commit: `596cfe9054d083aad53638709766c1e091284ea6`
- Source fix: `be1069d5ca9efa3b97dcdc5b8c0b3b0b7cbf3973`
- Regression smoke: `58d9caa9fcbe5e6499b9a19d4c4bf6c02c236918`
- Smoke registration: `829914c2e07253a60999654f0dc8f0fb40fa4582`
- Priority: P1 — non-canonical XML node kinds could load successfully and then be silently discarded or rewritten on template save.

## Confirmed defect

`TemplateProfileXmlSchemaValidator.ValidateElement()` accepted `XCData` through its `XText` branch because LINQ to XML `XCData` derives from `XText`. Whitespace-only CDATA could therefore be accepted in serializer-owned container positions even though the template serializer never emits CDATA.

`TemplateProfileStore.Load()` also passed only `document.Root` to the validator. Unsupported document-level comments or processing instructions were not inspected, yet `Serialize()` does not emit them, so load/save could silently discard those nodes.

Existing strict-schema and XML-text-preflight lanes were distinct: they cover element/attribute/schema structure and XML-valid application text, not unsupported LINQ-to-XML node kinds outside/inside serializer-owned structure.

## Completed change

- `TemplateProfileXmlSchemaValidator.Validate()` now inspects `root.Document` and rejects every document-level sibling node other than the root element.
- `ValidateElement()` now rejects `XCData` before generic `XText` handling.
- `TemplateProfileStore.cs` required no change because roots loaded by the production path remain attached to their `XDocument`.
- XML declarations remain accepted because they are not members of `XDocument.Nodes()`.
- Existing schema/order/attribute/namespace validation, template semantics, serialization format, and atomic save behavior remain unchanged.

## Regression coverage

`TemplateProfileXmlNodeCanonicalitySmoke` exercises the production `TemplateProfileStore.Load()` path using real temporary template files and covers:

- canonical minimal template XML still loads and preserves profile identity;
- XML declaration remains accepted;
- root-level whitespace CDATA is rejected;
- document-level comment is rejected;
- document-level processing instruction is rejected.

A module initializer registers the smoke with the existing Core smoke-test assembly.

## Readback verification

Read back from `main` at `829914c2e07253a60999654f0dc8f0fb40fa4582`: the document-node guard remains before root schema validation, the CDATA guard remains before `XText`, and both smoke files are present.

## Validation boundary

No GitHub Actions, local build/smoke execution, or BricsCAD runtime PASS is claimed. Verification in this lane is source/regression registration plus post-write `main` readback only.