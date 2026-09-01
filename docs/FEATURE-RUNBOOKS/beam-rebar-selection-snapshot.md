# Beam Rebar selection snapshot / UI failure qualification

Scope: `QS3DBEAMREBAR3D` on licensed BricsCAD V25. Remote/source agents may validate source guards and compile gates, but must not report these runtime scenarios as `LOCAL_PASS`.

Candidate rule: execute only against the exact pushed SHA recorded by the carrier. Start from a cold BricsCAD process unless the scenario explicitly says otherwise.

## Source contract

`QS3DBEAMREBAR3D` acquires selection exactly once through `CadSelectionGuard.AcquireCurrentSelection(document)` before canonical project mutation binding. Empty/cancel returns before mutation. The command derives read-only preview Beam targets, pins ProjectId/ChangeVersion and target identity, revalidates them on the canonical existing project, then passes the exact admitted `ObjectId[]` to `BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds)`. The builder clones the array before native traversal and must not call `SelectImplied`, `GetSelection`, `SetImpliedSelection`, or otherwise prompt/re-read editor selection.

Generated-rebar ownership validation, destructive replacement refusal, geometry constraints, project rollback and CAD transaction ordering remain unchanged. Semantic/audit publication remains before CAD transaction commit. Command failures and post-commit UI-sync warnings must not expose raw host/native exception messages.

## Licensed V25 matrix

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| BR01 | Existing project, one valid semantic Beam LINE with `RebarNotation` selected | Command creates/replaces expected longitudinal bars and reports success count. |
| BR02 | Empty/cancel selection | No canonical project mutation binding occurs; stable selection guidance is shown. |
| BR03 | PICKFIRST empty, interactive selection supplies valid Beam LINE | Interactive admission succeeds once and native generation consumes that admitted snapshot without a second prompt. |
| BR04 | Capture admitted selection, then perturb implied editor selection while native generation enters | Generation follows the originally admitted snapshot; later editor selection cannot redirect mutation. |
| BR05 | Change project ChangeVersion between preview admission and mutation binding | Command fails closed before native mutation without exposing raw exception detail. |
| BR06 | Change semantic source ownership/target set between preview and mutation binding | Target-set revalidation fails closed; no stale target is mutated. |
| BR07 | Existing generated-rebar ownership conflict | Destructive replacement is refused; no foreign solid is erased and user-visible failure remains redacted. |
| BR08 | Invalid LINE geometry, cover, notation, or layout causes builder failure before CAD commit | CAD transaction aborts and project snapshot is restored; no partial handles/audit state remains. |
| BR09 | Force palette refresh/editor regen failure after successful CAD/project commit | Generated bars remain committed; stable UI-sync warning is emitted without raw host exception text. |
| BR10 | Batch near `MaxBarsPerElement` / `MaxBarsPerBatch` | Existing bounded-generation limits remain enforced. |
| BR11 | Switch active drawing before invocation and run in the new drawing | Admission, preview, canonical project bind and native mutation stay on the invocation document; no cross-DWG mutation. |
| BR12 | Cold reopen after BR01/BR09 | Generated handles/count/diameter/cover/top-bottom/vertical snapshot, native ownership and audit remain coherent. |

## Remote-safe validation

Run the auto-discovered feature preflight suite and confirm `scripts/preflight-beam-rebar-selection-snapshot.py`, `scripts/preflight-rebar-selection-project-lifecycle.py` and `scripts/preflight-generated-rebar-audit.py` pass. Require fresh exact-head protected `preflight + core` SUCCESS before merge. Locked BricsCAD V25 compile/build evidence in protected CI is source qualification only, not licensed runtime evidence.

## Failure capture

For runtime failure, record exact candidate SHA, BricsCAD V25 build, drawing identity, scenario ID, admitted selection handles, project id/version, command-line output and whether CAD/project state changed. Preserve raw exception detail only in private diagnostic evidence when policy permits; the user-visible QS3D editor/palette surface must remain redacted.
