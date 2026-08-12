# Work claim — Project Browser workspace XML canonical content

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-browser-workspace-xml-canonicality`
- Registered: `2026-08-12T13:20:00+07:00`
- Baseline main SHA: `d9e3f56e5f29fbee38f943bab967a21470d20d29`
- Priority: P1 — non-canonical XML nodes can load successfully and then be silently rewritten or discarded on persistence.

## Confirmed defect

`ProjectBrowserWorkspaceStateStore.Deserialize()` validates the root subtree but not unsupported `XNode` siblings at the `XDocument` level. In addition, both `ValidateItemShape()` and `ValidateContainerNodes()` accept `XCData` through their `XText` checks because LINQ to XML `XCData` derives from `XText`.

Concrete counterexamples are document-level comments/processing instructions and whitespace-only CDATA. They can pass the current shape checks even though `Serialize()` never emits those representations, so a load/save cycle silently canonicalizes or removes input representation rather than rejecting it.

The completed Project Browser namespace/schema lanes are distinct; this claim is limited to unsupported XML node kinds and does not change semantic workspace state rules.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`, limited to document-node and CDATA validation
- focused Core smoke/preflight regression under `tests/QS3D.Core.SmokeTests/` or `tools/`
- this claim file

## Intended contract

- Reject CDATA before generic `XText` acceptance in item and container validation.
- Reject comments, processing instructions, and other unsupported `XNode` siblings outside the root element.
- Preserve the XML declaration contract; it is not an `XDocument.Nodes()` sibling.
- Preserve normal text content, incidental formatting whitespace accepted today, namespace/schema/order checks, semantic filters, selection state, and serialization format.
- Do not change Project Browser query/planning behavior or BricsCAD runtime/UI code.

## Validation boundary

No GitHub Actions, local build/smoke execution, or BricsCAD runtime PASS will be claimed unless actually observed.