# Work claim — Model Health numeric source smoke reconciliation

- Status: `COMPLETED`
- Agent: `codex-model-health-numeric-source-smoke-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T21:50:00+07:00`
- Baseline main SHA: `cd2d0d1023a2123aab0657299989984d1ef602a8`
- Priority: restore deterministic Core smoke after the completed numeric semantic-source liveness contract in `ebd4ca22` left one older smoke asserting the opposite behavior.

## Reserved scope

Reconcile only `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs`: source handle `0A` and live handle `A` must be treated as one canonical CAD Handle identity and must not emit `ORPHAN_HANDLE`.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/ComprehensiveGeneratedLiveHandleIdentitySmoke.cs`
- this claim for completion evidence

## Excluded scope

- No production `ModelHealthService`, generated-handle policy, P10/Workspace/Curtain, Source Reconcile/`LOCAL-004`, issue `#987`, workflow, release, BricsCAD runtime, or GitHub Actions changes.
- Preserve generated-live alias coverage and genuinely missing generated-handle failure coverage.

## Validation plan

- full Core Release build and deterministic smoke;
- focused source-health/static gates and aggregate preflight;
- `git diff --check`, latest-main sync, exact diff review, no force-push or branch deletion.

## Coordination

The completed Model Health numeric SourceHandle claim establishes the authoritative behavior. Current ACTIVE/BLOCKED claims and open PRs do not reserve this stale smoke reconciliation. P10 only surfaced the failure and remains out of scope.

## Completion condition

Claim-only PR is merged before test editing, the single-smoke correction is merged, exact validation is recorded, and this claim is marked `COMPLETED` in a separate closeout.

## Completion evidence

- Claim-only PR `#1066` merged at `b27afdae5f1b580873886ffdb6f2852e4d8b8620` before implementation editing began.
- Implementation commit `43035744e7d146eb1446af1a5d6c04a39cc1c12b` merged through PR `#1067` at `18d3693f229d15746d414c4d3f94778c8f485adc`.
- Core Release build PASS with 0 warnings / 0 errors; generated-handle ownership and semantic SourceHandle numeric-identity focused gates PASS; aggregate preflight PASS at 774/774; `git diff --check` PASS.
- Full Core smoke no longer reported the stale `ORPHAN_HANDLE` assertion and progressed to the separately owned `WorkspaceCurtainOwnerSelectionSmoke` `HANDLE:00D2` fixture blocker; that unrelated fixture was not changed in this lane.
- Only `ComprehensiveGeneratedLiveHandleIdentitySmoke.cs` changed. No production/P10 automation/workflow files changed; no BricsCAD runtime or GitHub Actions were run.
