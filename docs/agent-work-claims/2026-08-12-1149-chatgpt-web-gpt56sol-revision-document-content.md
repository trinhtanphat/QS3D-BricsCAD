# Work claim — Revision snapshot document-level XML content

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:49:00+07:00`
- Baseline main SHA: `aabe19dc5eebba962f1715212f7310946a2c7bfc`
- Priority: continue-all remote-safe persisted-state integrity

## Confirmed defect

`RevisionSnapshotStore.Load(...)` obtains the loaded document root and passes that `XElement` into `RevisionSnapshotXmlSchemaValidator`. The previous validator inspected only the root subtree, so document-level XML nodes such as comments or processing instructions outside the root were never validated. Loading succeeded, while `RevisionSnapshotStore.Serialize(...)` creates a fresh `XDocument` containing only the canonical root, so those unsupported nodes were silently discarded on a later save.

## Reserved scope

Fail closed on unsupported document-level XML nodes in revision snapshots while retaining XML declaration behavior and all existing root/subtree schema validation.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs`
- `scripts/preflight-revision-snapshot-schema.py`
- this claim file

The initially considered `RevisionSnapshotStore.cs` change was not required: the loaded root retains its `XDocument` through `root.Document`, allowing validation to cover document siblings without rewriting the store's production load flow.

## Excluded scope

- No revision compare/report semantic changes.
- No QSDB project persistence changes.
- No BricsCAD runtime/UI changes.
- No GitHub Actions dispatch.

## Validation performed

- Added document-level validation through `root.Document.Nodes()` before root schema parsing.
- The root element itself is the only accepted materialized document node; comments, processing instructions, or other sibling `XNode` values now throw `InvalidDataException`.
- XML declaration behavior is unchanged because declarations are represented separately from `XDocument.Nodes()`.
- Kept existing exact-name/no-namespace, required-container, and CDATA-before-XText guards intact.
- Updated `scripts/preflight-revision-snapshot-schema.py` to require the document guard, its error path, and ordering before root-schema parsing.
- Re-read validator and preflight from moving `main` after the writes and confirmed all guards remain present.
- No GitHub Actions, .NET build, or BricsCAD runtime validation was run.

## Completion

- Claim commit: `89d0d523658fe3fae4d5b23650e5e68af4cb23fa`
- Intermediate superseded implementation step: `2f2dfbe6b84f372b0b18e1ab5470c9b89dc8d270`
- Canonical implementation commit: `488fc84811f75e7ee435dcfb7f6ef3ce6851bc8e`
- Regression/preflight commit: `e91ff84d42343d6ef1bb931a4a10cd179f345830`
- Remaining runtime/local gates: run the focused preflight/.NET smoke locally if release qualification requires executable validation.
