# Review modeless publication / recognition failure qualification

Scope: licensed BricsCAD V25 Review surfaces (`QS3DBBSVIEW`, `QS3DRECOGNIZE*`, `QS3DREVDIFF`). Remote/source agents may validate source guards and compile gates, but must not report the runtime scenarios below as `LOCAL_PASS`.

Candidate rule: execute only against the exact pushed SHA recorded by issue #5096 / its canonical PR. Start from a cold BricsCAD process unless a scenario explicitly tests reentrancy in an already-running host.

## Source contract

Each Review surface owns two explicit lifecycle slots: pending host-show and published loaded window. `ShowAndPublish` captures exact native database identity plus the weak managed Document wrapper, reserves the exact owner in pending state before calling `Application.ShowModelessWindow`, attaches terminal cleanup before host show, confirms `IsLoaded` and exact pending ownership, then promotes pending to published. Reentrant invocation while pending must reuse/fail closed and must never construct/show a second candidate. Failed show and `Closed` callbacks release only the exact matching owner.

Existing BBS detached-preview behavior, Recognition strict/best-effort atomic commit/rollback semantics, Revision current-project requirements, locate semantics and generated-source filtering remain unchanged. Recognition post-commit UI refresh and rollback failures use stable user-facing messages and never append raw host/native exception messages.

## Licensed V25 matrix

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| RV01 | Cold `QS3DBBSVIEW` on valid project | One BBS window reaches loaded/published state; no duplicate window. |
| RV02 | Reinvoke BBS while first host-show is pending | Same exact owner is reused/fail-closed; no second candidate is shown. |
| RV03 | Reinvoke BBS after loaded in same drawing | Existing window activates; no replacement instance. |
| RV04 | Open BBS in drawing A, invoke from drawing B | Previous loaded owner closes terminally before replacement; close failure prevents second instance. |
| RV05 | Force host show failure before loaded | Pending owner is released and unpublished candidate is best-effort closed; next invocation can recover. |
| RV06 | Close loaded BBS/Recognition/Revision | Only the exact matching published owner is released. |
| RV07 | Recognition strict apply commits, palette refresh throws | Semantic commit remains; editor shows stable UI warning with no raw exception detail. |
| RV08 | Recognition auto apply commits, palette refresh throws | Commit remains; stable UI warning, no raw exception detail. |
| RV09 | Recognition auto batch throws during commit | Batch rollback semantics remain atomic; stable rollback message, no partial semantic capture and no raw exception detail. |
| RV10 | Reentrant Recognition while pending | No duplicate recognition review window and no second host show. |
| RV11 | Reentrant Revision diff while pending | No duplicate revision window and no second host show. |
| RV12 | Multi-DWG close/reopen cycle | Native database + managed wrapper affinity prevents cross-DWG reuse; no stale static owner remains after terminal close. |

## Remote-safe validation

Run the auto-discovered feature preflight suite and confirm `scripts/preflight-review-modeless-single-instance.py` and `scripts/preflight-review-publication-redaction.py` pass. Require fresh exact-head protected `preflight + core` SUCCESS before merge. Locked-reference BricsCAD V25 compile evidence is source qualification only, not licensed runtime evidence.

## Failure capture

For any licensed runtime failure, record exact candidate SHA, BricsCAD V25 build, drawing identity/native database identity, scenario ID, whether a pending or published owner existed, visible window count, command-line output and whether semantic/project state changed. Preserve raw exception detail only in private diagnostic evidence when policy permits; Editor/Palette surfaces must remain redacted.
