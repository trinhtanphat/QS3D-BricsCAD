# Work claim — Save identity scalar-revision gate reconciliation

- Status: `ACTIVE`
- Agent: `codex-save-identity-scalar-gate-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T21:45:00+07:00`
- Baseline main SHA: `5e5866c47df6586f685665b7af762fc92f57b96f`
- Priority: restore aggregate source-preflight truth after the scalar-owned drawing-identity revision change in `ace0a1f8` / companion gate update in `83b0d4b7`.

## Reserved scope

Reconcile only `scripts/preflight-project-save-identity-preflight-atomicity.py`: replace its stale expectation that adapter synchronizers call `project.Touch()` with the current contract that persisted `DrawingPath` / `DrawingFingerprint` scalar assignments own revision increments, and reject reintroduction of adapter-owned `Touch`.

## Expected surfaces

- `scripts/preflight-project-save-identity-preflight-atomicity.py`
- focused companion gate `scripts/preflight-project-context-drawing-identity-touch-order.py`
- this claim for completion evidence

## Excluded scope

- No production source changes, P10/Workspace/Curtain files, Source Reconcile/`LOCAL-004`, issue `#987`, BricsCAD runtime, workflow, release, or GitHub Actions work.
- No broad save/persistence redesign; the existing guard ordering and Store dispatch assertions remain unchanged.

## Validation plan

- run the reconciled save-identity gate and companion scalar-revision gate;
- run aggregate preflight and `git diff --check` on the exact candidate;
- review current-main movement before commit/push/merge; no force-push or branch deletion.

## Coordination

Current ACTIVE/BLOCKED claims and open PRs were inspected at the baseline. None reserves this stale gate. The locally surfaced P10 failure is only the reporter; P10 remains out of scope.

## Completion condition

Claim-only PR is merged before editing, the narrow gate correction is merged to current main, exact validation is recorded truthfully, and this claim is marked `COMPLETED` in a separate closeout.
