# Column Tie selection ownership / UI failure qualification

Scope: `QS3DREBARTIES3D` on licensed BricsCAD V25. Remote/source agents may validate source guards and compile gates, but must not report these runtime scenarios as `LOCAL_PASS`.

Candidate rule: execute only against the exact pushed SHA recorded by the carrier. Start from a cold BricsCAD process unless the scenario explicitly says otherwise.

## Source contract

The command must not pre-read implied selection. `ColumnTieSolidBuilder.BuildSelected` owns the single `Editor.SelectImplied()` call at the native mutation boundary. Empty/no usable selection returns zero and the command shows the existing selection guidance. Existing-project requirement, generated-tie ownership verification, per-element/per-batch bounds, project rollback and CAD transaction semantics remain unchanged. Command failures and UI-sync warnings must not expose raw host/native exception messages.

## Licensed V25 matrix

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| CT01 | Existing project, one valid semantic Column rectangle selected | Command creates/replaces expected ties; success count is shown. |
| CT02 | No implied selection | No mutation; stable selection guidance is shown. |
| CT03 | Mixed pickset where only one object maps to a Column semantic | Only mapped semantic source is processed; unrelated objects are ignored. |
| CT04 | Change pickset immediately before command execution | Generation follows the one pickset observed by the builder at mutation boundary; no evidence of a second command-side selection read. |
| CT05 | Invalid/non-rectangular selected semantic source | Native/project transaction fails closed; previous generated ownership is not silently replaced by partial output. User-facing failure copy contains no raw exception detail. |
| CT06 | Existing generated-tie ownership conflict | Destructive replacement is refused by ownership guard; no foreign solid is erased. Failure copy remains redacted. |
| CT07 | Force a UI refresh/regen failure after successful CAD/project commit | Generated ties remain committed; editor receives stable UI-sync warning without raw host exception text. |
| CT08 | Batch near configured tie limits | Existing per-element and batch bounds remain enforced; no unbounded solid generation. |
| CT09 | Switch active drawing before invoking command, then run in the new active drawing | Command operates only on the active document/project selected at invocation; no cross-DWG mutation is observed. |
| CT10 | Cold reopen after CT01/CT07 and inspect generated ownership/semantic properties | Generated handles/count/spacing/cover snapshot and ownership remain coherent after reopen. |

## Remote-safe validation

Run the auto-discovered feature preflight suite and confirm `scripts/preflight-column-tie-selection-snapshot.py` passes. Run repository protected Shared CI for the exact candidate and require current protected `preflight + core` SUCCESS before merge. Do not convert static/preflight success into licensed runtime evidence.

## Failure capture

For a runtime failure, record exact candidate SHA, BricsCAD V25 build, drawing identity, scenario ID, command-line output and whether CAD/project state changed. Preserve raw exception detail only in private diagnostic evidence when policy permits; the user-visible QS3D editor/palette surface must remain redacted.
