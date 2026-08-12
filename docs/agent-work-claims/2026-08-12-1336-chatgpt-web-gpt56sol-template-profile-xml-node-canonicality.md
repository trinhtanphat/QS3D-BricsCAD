# Work claim — Template Profile XML node canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-profile-xml-node-canonicality`
- Registered: `2026-08-12T13:36:00+07:00`
- Baseline main SHA: `1ccd128f538aa2215a9599af6d30d4bd5f3baa3c`
- Priority: P1 — non-canonical XML node kinds can load successfully and then be silently discarded or rewritten on template save.

## Confirmed defect

`TemplateProfileXmlSchemaValidator.ValidateElement()` accepts `XCData` through its `XText` branch because LINQ to XML `XCData` derives from `XText`. Whitespace-only CDATA can therefore be accepted in serializer-owned container positions even though the template serializer never emits CDATA.

`TemplateProfileStore.Load()` also passes only `document.Root` to the validator. Unsupported document-level comments or processing instructions are not inspected, yet `Serialize()` does not emit them, so load/save can silently discard those nodes.

Existing strict-schema and XML-text-preflight lanes are distinct: they cover element/attribute/schema structure and XML-valid application text, not unsupported LINQ-to-XML node kinds outside/inside serializer-owned structure.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileXmlSchemaValidator.cs`, limited to XML node-kind validation
- `src/QS3D.Core/Templates/TemplateProfileStore.cs`, limited to document-level validator wiring if required
- focused smoke/preflight regression under `tests/QS3D.Core.SmokeTests/` or `scripts/`
- this claim file

## Intended contract

- Reject `XCData` before generic `XText` acceptance.
- Reject comments, processing instructions, and other unsupported `XNode` siblings outside the root element.
- Preserve XML declaration compatibility.
- Preserve current schema, canonical ordering, attribute/namespace checks, template semantics, serialization format, and atomic save behavior.

## Validation boundary

No GitHub Actions, local build/smoke execution, or BricsCAD runtime PASS will be claimed unless actually observed.