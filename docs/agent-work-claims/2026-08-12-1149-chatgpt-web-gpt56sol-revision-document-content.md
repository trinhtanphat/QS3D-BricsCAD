# Work claim — Revision snapshot document-level XML content

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:49:00+07:00`
- Baseline main SHA: `aabe19dc5eebba962f1715212f7310946a2c7bfc`
- Priority: continue-all remote-safe persisted-state integrity

## Confirmed defect

`RevisionSnapshotStore.Load(...)` currently calls `LoadDocument(path).Root` and passes only the root `XElement` into `RevisionSnapshotXmlSchemaValidator`. Document-level XML nodes such as comments or processing instructions outside the root therefore never reach validation. Loading succeeds, while `RevisionSnapshotStore.Serialize(...)` creates a fresh `XDocument` containing only the canonical root, so those unsupported nodes are silently discarded on a later save.

## Reserved scope

Fail closed on unsupported document-level XML nodes in revision snapshots while retaining the XML declaration behavior and all existing root/subtree schema validation.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- `src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs`
- `scripts/preflight-revision-snapshot-schema.py`
- this claim file

## Excluded scope

- No revision compare/report semantic changes.
- No QSDB project persistence changes.
- No BricsCAD runtime/UI changes.
- No GitHub Actions dispatch.

## Validation plan

- Preserve the loaded `XDocument` through validation instead of discarding everything outside `.Root` first.
- Accept only the single root element at document-node level; reject comments, processing instructions, or any other materialized sibling node.
- Keep XML declaration unaffected because it is represented separately from `XDocument.Nodes()`.
- Update the focused revision-schema preflight to lock document-level validation before snapshot field parsing.
- Re-read moving `main` after writes and close with exact commit evidence.
