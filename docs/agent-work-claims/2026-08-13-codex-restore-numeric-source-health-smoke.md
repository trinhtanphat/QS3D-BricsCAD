# Work claim — Restore numeric source-health smoke contract

- Status: `ACTIVE`
- Agent: `codex-restore-numeric-source-health-smoke-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T22:00:00+07:00`
- Baseline main SHA: `cd3a8eb17bf6dace92224c17cad826178bd78933`
- Priority: current clean full Core smoke is red because direct commits `f8c6d489` / `f477ca54` contradicted the completed numeric SourceHandle identity contract, removed genuinely-missing source coverage, and reverted production diagnostic normalization.

## Reserved scope

Restore `ModelHealthService` numeric SourceHandle identity for stored-source duplicate/cross-owner diagnostics and live-source liveness. Restore the comprehensive smoke so numeric aliases (`0A` persisted / `A` live) do not report `ORPHAN_HANDLE`, while a genuinely different source/live pair still reports `ORPHAN_HANDLE`. Keep the existing focused numeric SourceHandle smoke read-only unless it needs a minimal assertion adjustment demonstrated by the restored contract.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs` (read-only unless evidence requires a minimal test-only adjustment)
- `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs`
- this claim for completion evidence

## Excluded scope

- No ownership-policy changes, P10 automation/Workspace/Curtain files, Source Reconcile/`LOCAL-004`, issue `#987`, workflows/releases, BricsCAD runtime, or GitHub Actions.
- Preserve generated live-alias and genuinely missing generated-handle coverage.

## Validation plan

- clean full Core Release rebuild and deterministic smoke;
- focused generated-handle and semantic SourceHandle numeric-identity gates;
- aggregate preflight and `git diff --check`;
- sync/review latest main before commit/push/merge, with no force-push or branch deletion.

## Coordination

The completed `2026-08-13-1933` claim defines numeric semantic-source duplicate, cross-owner, and liveness identity. Direct commits `f8c6d489` and `f477ca54` have no PR/claim and conflict with that contract. Parent coordination explicitly assigns this expanded restoration. Current ACTIVE/BLOCKED claims and open PRs do not reserve these surfaces.

## Completion condition

This claim expansion is merged before production editing, numeric production identity plus the two-case comprehensive smoke contract are restored and merged, clean full Core smoke/aggregate results are recorded, and this claim is marked `COMPLETED` in a separate closeout.
