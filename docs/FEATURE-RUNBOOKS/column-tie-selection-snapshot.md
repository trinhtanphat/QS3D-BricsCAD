# Column Tie selection snapshot / UI failure qualification

Scope: `QS3DREBARTIES3D` on licensed BricsCAD V25. Remote/source agents may validate source guards and compile gates, but must not report these runtime scenarios as `LOCAL_PASS`.

Candidate rule: execute only against the exact pushed SHA recorded by the carrier. Start from a cold BricsCAD process unless the scenario explicitly says otherwise.

## Source contract

`QS3DREBARTIES3D` captures PICKFIRST exactly once through `CadSelectionGuard.ReadImpliedSelection(document)` before canonical project binding. Empty selection returns before `ExistingProjectMutationContext.Require`, preserving the established no-project-binding-on-empty lifecycle. The exact admitted `ObjectId[]` snapshot is then passed to `ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds)`, and the builder must not call `SelectImplied` or otherwise re-read selection. The builder clones the supplied array before native traversal so its mutation set is stable for the operation.

Existing-project requirement, generated-tie ownership verification, per-element/per-batch bounds, project rollback and CAD transaction semantics remain unchanged. Command failures and post-commit UI-sync warnings must not expose raw host/native exception messages.

## Licensed V25 matrix

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| CT01 | Existing project, one valid semantic Column rectangle selected | Command creates/replaces expected ties; success count is shown. |
| CT02 | No implied selection | No project binding/mutation occurs; stable selection guidance is shown. |
| CT03 | Mixed pickset where only one object maps to a Column semantic | Only the admitted mapped semantic source is processed; unrelated objects are ignored. |
| CT04 | Capture PICKFIRST, then perturb editor implied selection while native generation is entering | Generation follows the admitted snapshot passed to the builder; no second selection read changes the mutation set. |
| CT05 | Invalid/non-rectangular selected semantic source | Native/project transaction fails closed; previous generated ownership is not silently replaced by partial output. User-facing failure copy contains no raw exception detail. |
| CT06 | Existing generated-tie ownership conflict | Destructive replacement is refused by ownership guard; no foreign solid is erased. Failure copy remains redacted. |
| CT07 | Force a UI refresh/regen failure after successful CAD/project commit | Generated ties remain committed; editor receives stable UI-sync warning without raw host exception text. |
| CT08 | Batch near configured tie limits | Existing per-element and batch bounds remain enforced; no unbounded solid generation. |
| CT09 | Switch active drawing before invoking command, then run in the new active drawing | PICKFIRST/project binding/build all use the active document selected at invocation; no cross-DWG mutation is observed. |
| CT10 | Cold reopen after CT01/CT07 and inspect generated ownership/semantic properties | Generated handles/count/spacing/cover snapshot and ownership remain coherent after reopen. |

## Remote-safe validation

Run the auto-discovered feature preflight suite and confirm both `scripts/preflight-column-tie-selection-snapshot.py` and the historical rebar selection/project lifecycle guard pass. Run repository protected Shared CI for the exact candidate and require current protected `preflight + core` SUCCESS before merge. Do not convert static/preflight success into licensed runtime evidence.

## Failure capture

For a runtime failure, record exact candidate SHA, BricsCAD V25 build, drawing identity, scenario ID, admitted PICKFIRST handles, command-line output and whether CAD/project state changed. Preserve raw exception detail only in private diagnostic evidence when policy permits; the user-visible QS3D editor/palette surface must remain redacted.
