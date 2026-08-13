# Work claim — Restore numeric source-health smoke contract

- Status: `ACTIVE`
- Agent: `codex-restore-numeric-source-health-smoke-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T22:00:00+07:00`
- Baseline main SHA: `cd3a8eb17bf6dace92224c17cad826178bd78933`
- Priority: current clean full Core smoke is red because direct commit `f8c6d489` contradicted the completed numeric SourceHandle liveness contract and removed genuinely-missing source coverage.

## Reserved scope

Restore only `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs` so numeric aliases (`0A` persisted / `A` live) do not report `ORPHAN_HANDLE`, while a genuinely different source/live pair still reports `ORPHAN_HANDLE`.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs`
- this claim for completion evidence

## Excluded scope

- No production `ModelHealthService` or ownership-policy changes, P10 automation/Workspace/Curtain files, Source Reconcile/`LOCAL-004`, issue `#987`, workflows/releases, BricsCAD runtime, or GitHub Actions.
- Preserve generated live-alias and genuinely missing generated-handle coverage.

## Validation plan

- clean full Core Release rebuild and deterministic smoke;
- focused generated-handle and semantic SourceHandle numeric-identity gates;
- aggregate preflight and `git diff --check`;
- sync/review latest main before commit/push/merge, with no force-push or branch deletion.

## Coordination

The completed `2026-08-13-1933` claim and production source define numeric semantic-source liveness. Direct commit `f8c6d489` has no PR/claim and conflicts with that contract. Current ACTIVE/BLOCKED claims and open PRs do not reserve this smoke restoration.

## Completion condition

Claim-only PR is merged before editing, the two-case smoke contract is restored and merged, clean full Core smoke/aggregate results are recorded, and this claim is marked `COMPLETED` in a separate closeout.
