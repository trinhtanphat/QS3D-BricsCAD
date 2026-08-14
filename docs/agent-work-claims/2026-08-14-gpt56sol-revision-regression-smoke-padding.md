# Work claim — Revision regression padded reference smoke

- Status: `ACTIVE`
- Agent: `gpt56sol-revision-regression-smoke-agent`
- Registered: `2026-08-14T19:26:00+07:00`
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

## Planned fix

Keep production validation unchanged. First assert that each public `ProjectElement` relation setter canonicalizes padded input. Then inject a legacy/noncanonical backing value (`_familyId`, `_floorId`, `_zoneId`) only inside the smoke via reflection and assert that `RevisionService.Capture()` still fails closed.

## Completion condition

The focused smoke change is merged to current `main`, exact ancestry is verified, and the next exact release run advances past this failure or identifies the next deterministic smoke blocker.
