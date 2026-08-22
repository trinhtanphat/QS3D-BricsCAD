# Work claim — Revision snapshot CDATA canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:42:00+07:00`
- Baseline main SHA: `0a298f9b68136a970b09dfa5bd6f850598df1b4b`
- Priority: continue-all remote-safe persisted-state integrity

## Confirmed defect

`RevisionSnapshotXmlSchemaValidator.ValidateElement(...)` previously checked `XText` before distinguishing `XCData`. Because `XCData` derives from `XText`, whitespace-only CDATA was accepted as ordinary ignorable whitespace. `RevisionSnapshotStore.Load(...)` does not preserve those nodes and the serializer never emits CDATA, so malformed current-schema revision XML could load successfully and later be rewritten into a different representation.

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

## Validation performed

- Inserted explicit `XCData` rejection before the general `XText` branch.
- Preserved the existing acceptance of ordinary formatting whitespace text.
- Updated `scripts/preflight-revision-snapshot-schema.py` to require the CDATA guard and assert it appears before the `XText` branch.
- Re-read both source and preflight from moving `main` after the writes; both guards remain present.
- No GitHub Actions, .NET build, or BricsCAD runtime validation was run.

## Completion

- Claim commit: `5c51b92b4b4c9accfb18cc5026dad690cbb95207`
- Implementation commit: `099e3fd46758e3d2c16c05e833016bdcf1aab8e9`
- Regression/preflight commit: `2473c218529991033434b311cf0da25aad6a2170`
- Remaining runtime/local gates: run the focused preflight/.NET smoke locally if release qualification requires executable validation.
