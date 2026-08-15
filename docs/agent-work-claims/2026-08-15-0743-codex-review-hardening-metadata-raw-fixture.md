# Work claim — Review hardening raw metadata fixture

- Status: `ACTIVE`
- Agent: `codex-local-worker` (`/root`)
- Registered: `2026-08-15T07:43:00+07:00`
- Baseline main SHA: `d06328d6ea8a6f385031b7e786116b7eefce270e`
- Priority: `Core smoke / QSDB persistence defense-in-depth`

## Diagnosis

`ReviewHardeningSmoke.QsdbRejectsUnsavableMutableState` still assigns an empty metadata key through the public `ProjectMetadataDictionary` indexer. The completed generic metadata persistability contract now correctly rejects that key at the supported mutation boundary, so the fixture never reaches its independent assertion that `QsdbProjectStore.Save` rejects already-corrupt raw in-memory state without replacing the destination file.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ReviewHardeningSmoke.cs` — assert the public empty-key write rejects, then inject the same raw key through the private metadata backing dictionary with test-local reflection, assert the injection is visible, and retain the Save `InvalidDataException` plus exact destination no-replacement check.
- this claim file — claim-first registration and exact closeout evidence.

## Exclusions

- No production `ProjectMetadataDictionary`, `QsdbProjectStore`, schema, persistence, metadata, revision, native BricsCAD, LOCAL runner, release, workflow, private-data or GitHub Actions change.
- No change to the remaining review-hardening cases beyond the one unreachable setup boundary.

## Validation

- Merge this claim before implementation and re-fetch current claims/open PRs.
- Build `QS3D.Core` and `QS3D.Core.SmokeTests` in Release, run the full registered Core smoke suite, relevant QSDB/static gates, and `git diff --check`.
- Publish/merge the bounded test-only implementation normally, then mark this claim `COMPLETED` with exact SHAs and the next independently observed blocker, if any.
