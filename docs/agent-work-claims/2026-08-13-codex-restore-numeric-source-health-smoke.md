# Work claim — Restore numeric source-health smoke contract

- Status: `COMPLETED`
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

## Completion evidence

- Original claim-only PR `#1072` merged at `71d5acb2e9240e06d65245dd52203e20e2bc62b7` before smoke editing. After direct production commit `f477ca54` widened the regression, claim-expansion PR `#1073` merged at `939d858183b1fab7e9026f716c883a4908dc8776` before any production edit.
- Implementation commit `a4b854d1e81355ce35157932443e16761c734988` merged through PR `#1074` at `e7318dc41bc04b26bee9f5b4f6b985d38144c9bd`.
- Exact merge SHA clean full Core smoke: `ALL PASS`; Core Release build: 0 warnings / 0 errors.
- Generated-handle ownership, semantic SourceHandle numeric-identity, and Auto Room canonical-selection focused gates PASS; aggregate preflight PASS at 774/774; `git diff --check` PASS.
- `ModelHealthSourceHandleSmoke.cs` required no edit. Production numeric source identity and comprehensive numeric-alias / true-missing orphan coverage are coherent again.
- No P10 automation/Workspace/Curtain/workflow files changed; no BricsCAD runtime or GitHub Actions were run.
