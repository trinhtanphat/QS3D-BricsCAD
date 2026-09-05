# V25/V26 Workspace route transient-retry qualification

## Scope

Issue #5816 hardens only `BltBimWorkspaceActivationCoordinator` tab-transition publication. HOME, PROJECT, BIM, Start Center, Project Information, Workspace palettes and shell chrome retain their existing authorities.

Hosted/source evidence is `REMOTE_SAFE` only. A successful V25/V26 build or source guard is not licensed BricsCAD runtime PASS.

## Deterministic source contract

On the exact candidate SHA:

1. `scripts/preflight-v25-workspace-route-retry.py` must pass.
2. `_lastTabId` is published only after the observed tab route completes without throwing.
3. A transient exception therefore leaves the route pending for the next timer tick rather than suppressing all retries until another tab transition.
4. BIM keeps its bounded settle ticks; non-QS3D tabs still hide the HOME/PROJECT special surfaces; the outer polling callback remains fail-soft.
5. No new BricsCAD event dependency or duplicate palette/window authority is introduced.

## LOCAL_ONLY matrix

Freeze the exact pushed SHA, adapter/Core hashes and licensed host version before execution. Use a disposable profile/drawing and restore all UI/profile state afterwards.

Run independently on licensed BricsCAD V25 and V26 where available:

- HOME -> PROJECT -> BIM -> normal CAD tab -> HOME, verifying the intended single large QS3D surface after each transition;
- repeat rapid transitions while Ribbon/workspace chrome is being reconstructed;
- with an approved diagnostic/failure-injection build or host hook, make one HOME route and one PROJECT route throw once before publication, then prove the next bounded poll converges without another user tab change;
- verify BIM still performs only its bounded settle reassertions and does not create duplicate palettes;
- verify Stop/plugin teardown removes the timer callback and leaves no owned modeless/UI residue;
- repeat after workspace rebuild and after document switch; confirm no document/project mutation occurs;
- record sanitized exact-SHA PASS/FAIL/NO_RESULT plus cleanup evidence.

Do not claim the failure-injection rows from source inspection. If deterministic failure injection is unavailable on the licensed host, record those rows `NO_RESULT`, not PASS.
