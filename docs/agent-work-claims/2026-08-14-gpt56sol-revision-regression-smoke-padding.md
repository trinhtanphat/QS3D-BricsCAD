# Work claim — Revision regression padded reference smoke

- Status: `COMPLETED` — duplicate claim; no source/test change from this lane
- Agent: `gpt56sol-revision-regression-smoke-agent`
- Registered: `2026-08-14T19:26:00+07:00`
- Closed: `2026-08-14T19:32:00+07:00`
- Baseline main SHA: `a58e71241f6910d66ceccb8e331d78231bd8f48e`
- Trigger: release #179 / run `31800005494`, deterministic Core smoke annotation from job `94765535555`

## Evidence

The exact CI annotation identifies `RevisionRegressionSmoke.CaptureRejectsPaddedReferenceIds()` as failing because it expected `InvalidOperationException` but none was thrown. `RevisionService.Capture()` still rejects genuinely non-canonical Family/Floor/Zone references. `ProjectElement` public relation setters now canonicalize surrounding whitespace, so the smoke no longer creates the malformed state it intends to test.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`
- this claim file

## Excluded scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- all production behavior, V25 adapter/runtime, #1005 Undo semantics, workflows and release policy

## Resolution

This claim was registered after the earlier claim #1322 had already reserved the exact same release #179 failure and file. The earlier owner implemented the fixture repair through #1323 and landed it to `main` through #1324 at merge commit `de99ca6be8695cde854d875f6c848ceaafcbc5b1`.

Therefore this newer lane is released as a duplicate. No implementation from `fix/gpt56sol-revision-regression-smoke-padding` should be merged. Fresh release evidence, not a second equivalent patch, determines the next blocker.
