# Work claim — Revision snapshot CDATA canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:42:00+07:00`
- Baseline main SHA: `0a298f9b68136a970b09dfa5bd6f850598df1b4b`
- Priority: continue-all remote-safe persisted-state integrity

## Confirmed defect

`RevisionSnapshotXmlSchemaValidator.ValidateElement(...)` checks `XText` before distinguishing `XCData`. Because `XCData` derives from `XText`, whitespace-only CDATA is accepted as ordinary ignorable whitespace. `RevisionSnapshotStore.Load(...)` does not preserve those nodes and the serializer never emits CDATA, so malformed current-schema revision XML can load successfully and later be rewritten into a different representation.

## Reserved scope

Fail closed on every CDATA node in revision snapshot XML while preserving ordinary formatting whitespace and all existing exact-name/no-namespace/container requirements.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs`
- `scripts/preflight-revision-snapshot-schema.py`
- this claim file

## Excluded scope

- No revision semantic compare/report changes.
- No QSDB project persistence changes.
- No BricsCAD runtime/UI changes.
- No GitHub Actions dispatch.

## Validation plan

- Reject `XCData` before the general `XText` branch.
- Keep ordinary whitespace text accepted for formatting.
- Update the existing focused revision-schema preflight to lock the explicit CDATA rejection ordering.
- Re-read moving `main` source/preflight after each write and close this claim with exact commit evidence.
