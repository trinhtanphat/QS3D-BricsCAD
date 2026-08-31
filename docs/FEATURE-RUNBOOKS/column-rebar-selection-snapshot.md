# Column Rebar selection snapshot / UI failure qualification

Scope: `QS3DREBAR3D` on licensed BricsCAD V25. Remote/source agents may validate source guards and compile gates, but must not report these runtime scenarios as `LOCAL_PASS`.

Candidate rule: execute only against the exact pushed SHA recorded by the carrier. Start from a cold BricsCAD process unless the scenario explicitly says otherwise.

## Source contract

`QS3DREBAR3D` captures PICKFIRST exactly once through `CadSelectionGuard.ReadImpliedSelection(document)` before canonical project mutation binding. Empty selection returns before project mutation. The command derives read-only preview targets from that admitted snapshot, pins ProjectId/ChangeVersion and semantic target identity, revalidates them on the canonical existing project, then passes the exact admitted `ObjectId[]` to `ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds)`. The builder clones the array before native traversal and must not call `SelectImplied` or otherwise re-read editor selection.

Generated-rebar ownership validation, destructive replacement refusal, geometry constraints, per-element/per-batch limits, project rollback and CAD transaction ordering remain unchanged. Semantic/audit publication remains before CAD transaction commit. Command failures and post-commit UI-sync warnings must not expose raw host/native exception messages.

## Licensed V25 matrix

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| CR01 | Existing project, one valid semantic Column rectangle with `RebarNotation` selected | Command creates/replaces expected vertical bars and reports success count. |
| CR02 | No implied selection | No project mutation binding occurs; stable selection guidance is shown. |
| CR03 | Mixed pickset where only one admitted object maps to Column semantic | Only the admitted mapped semantic source is processed; unrelated objects are ignored. |
| CR04 | Capture PICKFIRST, then perturb editor implied selection while native generation is entering | Generation follows the original admitted snapshot; no second selection read changes the mutation set. |
| CR05 | Change project ChangeVersion between preview admission and mutation binding | Command fails closed before native mutation and requests reselection without exposing raw exception detail. |
| CR06 | Change semantic source ownership/target set between preview and mutation binding | Target-set revalidation fails closed; no stale admitted target is mutated. |
| CR07 | Existing generated-rebar ownership conflict | Destructive replacement is refused; no foreign solid is erased and failure copy remains redacted. |
| CR08 | Invalid rectangle/geometry or notation causes builder failure before CAD commit | CAD transaction aborts and project snapshot is restored; no partial generated handles/audit state remains. |
| CR09 | Force palette refresh or editor regen failure after successful CAD/project commit | Generated bars remain committed; editor receives stable UI-sync warning without raw host exception text. |
| CR10 | Batch near `MaxBarsPerElement` / `MaxBarsPerBatch` | Existing bounds remain enforced; no unbounded solid generation. |
| CR11 | Switch active drawing before invocation and run in the new drawing | PICKFIRST, read-only preview, canonical project binding and native build all use the invocation document; no cross-DWG mutation. |
| CR12 | Cold reopen after CR01/CR09 | Generated handles/count/diameter/cover/vertical snapshot, native ownership and audit remain coherent. |

## Remote-safe validation

Run the auto-discovered feature preflight suite and confirm `scripts/preflight-column-rebar-selection-snapshot.py`, `scripts/preflight-rebar-selection-project-lifecycle.py` and `scripts/preflight-generated-rebar-audit.py` pass. Require fresh exact-head protected `preflight + core` SUCCESS before merge. The locked BricsCAD V25 compile/build evidence in protected CI is source qualification only, not licensed runtime evidence.

## Failure capture

For a runtime failure, record exact candidate SHA, BricsCAD V25 build, drawing identity, scenario ID, admitted PICKFIRST handles, project id/version, command-line output and whether CAD/project state changed. Preserve raw exception detail only in private diagnostic evidence when policy permits; the user-visible QS3D editor/palette surface must remain redacted.
