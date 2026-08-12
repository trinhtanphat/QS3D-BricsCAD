# Work claim — Revision snapshot required elements section

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `cf333d168542f70678c351ee78e37770f19f7499`
- Priority: continue-all remote-safe persisted-state integrity

## Reserved scope

Make revision snapshot XML fail closed when the canonical root omits the required `<elements>` container. The current validator only enforces at-most-one, while `RevisionSnapshotStore.Load` interprets a missing container as an empty snapshot and `Save` always emits the container, causing lossy malformed-input normalization.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs`
- `scripts/preflight-revision-snapshot-schema.py`
- this claim file

## Excluded scope

- No revision comparison/report semantics changes.
- No QSDB project-store schema changes.
- No BricsCAD V25/V26 runtime/UI changes.
- No GitHub Actions dispatch.

## Validation planned

- Require exactly one root `<elements>` section while preserving existing exact-name/no-namespace validation.
- Update focused revision-schema preflight to lock missing and duplicate root-container rejection.
- Re-read source/preflight from current `main` before each write and verify the closeout commit remains on `main`.
