# Work claim — Revision snapshot required elements section

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `cf333d168542f70678c351ee78e37770f19f7499`
- Priority: continue-all remote-safe persisted-state integrity

## Reserved scope

Make revision snapshot XML fail closed when the canonical root omits the required `<elements>` container. The previous validator only enforced at-most-one, while `RevisionSnapshotStore.Load` interpreted a missing container as an empty snapshot and `Save` always emitted the container, causing lossy malformed-input normalization.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs`
- `scripts/preflight-revision-snapshot-schema.py`
- this claim file

## Excluded scope

- No revision comparison/report semantics changes.
- No QSDB project-store schema changes.
- No BricsCAD V25/V26 runtime/UI changes.
- No GitHub Actions dispatch.

## Validation performed

- Re-read current `RevisionSnapshotStore.Load` and confirmed a missing `<elements>` container was converted to an empty sequence while serialization always emits `<elements>`.
- Changed the revision XML validator to require exactly one root `<elements>` section, preserving exact-name/no-namespace validation and existing optional per-element singleton containers.
- Updated `scripts/preflight-revision-snapshot-schema.py` to require `RequireExactlyOne(root, "elements")`, reject regressions back to root `RequireAtMostOne`, and keep duplicate-root-container coverage through the exact-one count.
- Re-read both source and preflight from `main` after the writes.
- No GitHub Actions, .NET build, or BricsCAD runtime validation was run.

## Completion

- Claim commit: `e1aca3ee993164a6264b6c84837c34671b8c949e`
- Implementation commit: `d9cc1fd20e18a48a2a55dde273a04884ccbd6fe9`
- Regression/preflight commit: `a3a82e0c08a39e3685be8fdaa1c0c7b4b30ba300`
- Remaining runtime/local gates: run the focused preflight/.NET smoke locally if release qualification requires executable validation.
