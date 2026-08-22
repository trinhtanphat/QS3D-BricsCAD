# LOCAL-007 P01 — wall-junction read-only analysis qualification

Status: `COMPLETED`

Parent: #73 / LOCAL-007

Licensed qualification issue: #3596

Lane-Key: `issue-3596`

Qualification branch: `agent/codex/issue3596-v25-wall-junction-analysis-p01`

Exact runtime baseline: `8c928e1b17e1e161ef8437b56ad08aa9ef0e9b66`

## Boundary

This local-only P01 cell qualifies the production `QS3DWALLJUNCTIONS` selection and read-only analysis lifecycle in licensed BricsCAD V25. It covers exact-PID ESC from interactive selection, successful projectless LINE/open-POLYLINE analysis with source defaults, and cold-cache analysis against an existing project carrying custom junction, arc-sagitta and planarity tolerances. It does not change product source and does not qualify Wall Snap Preview/Apply, physical `Solid3d` materialization, multi-owner replacement, save/reopen, Undo/Redo or the broader LOCAL-007 matrix.

The run used only three disposable copies of the repository-public generated sample. The private probe, runner, raw command logs, drawing copies, sidecars and identifiers remain under ignored `artifacts/` or were deleted during cleanup. No customer/private drawing, raw Handle, ProjectId, ElementId, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed result

`LOCAL_PASS / BOUNDED_P01_ANALYSIS` was recorded on the exact clean pushed runtime candidate above with BricsCAD V25.2.10. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `EE188F0D2CF96E26F1622C5D1FC8046BB16D43BF0002DB18354F77FC9FE373F2` and `3E8CAAB10AF5E34945917F22AD03BED62624D9D60EAFC54F89F0A07DBF5D5F36`. The exact-source identity gate bound both candidate assemblies to the tested Git SHA.

The same self-contained run proved:

- exact-PID ESC returned the interactive no-PICKFIRST selection to idle before the source-guarded project read boundary, with no project cache, sidecar, CAD or selected-geometry mutation;
- projectless analysis accepted LINE plus open POLYLINE input, did not create a project/cache/sidecar/audit state, and emitted the independently planned production summary `L:1,T:1,X:1,Straight:1,End:16,Multi:1,SnapPlan:0`;
- the cold-cache valid-project path consumed custom `0.02 m` junction tolerance, `0.00075 m` arc sagitta and `0.001 m` planarity tolerance, including 27 independently tessellated bulged segments, and emitted `L:27,T:0,X:0,Straight:0,End:4,Multi:0,SnapPlan:1`;
- canonical ProjectId continuity held while project metadata, `ChangeVersion`, `UpdatedUtc`, audit content, CAD geometry and sidecar bytes stayed unchanged; the persistent `.qsdb.lock` file stayed byte-identical and could be reacquired exclusively after save and after both read-only checks;
- the two production summaries were recovered from their distinct BricsCAD per-document command logs and matched the independently planned fixture summaries exactly;
- all three disposable DWG files stayed byte-identical to the public fixture before discard, BricsCAD closed each without saving and exited gracefully with code `0`.

## Repository validation and safety

- `QS3D.BricsCAD.V25` Release|x64 build against the licensed V25 installation: `0 warnings / 0 errors`.
- Private probe Release build: `0 warnings / 0 errors`.
- `QS3D.Core.SmokeTests` execution: `ALL PASS`.
- `scripts/preflight-wall-junctions.py`, `scripts/preflight-wall-junction-selection.py` and `scripts/preflight-wall-junction-ownership.py`: PASS.
- Public fixture SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; the user's drawing bytes were unchanged and the drawing was never opened.
- Fixed per-user V25 DemandLoad path/bytes and `LoadCtrls=2` were preserved; no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Fixture copies, private sidecars and all three raw command logs were removed; zero BricsCAD processes remained and the tracked worktree was clean.

## Remaining scope

Issue #3596 closes only this bounded P01 analysis cell. Parent #73 and overall LOCAL-007 remain open for canonical Wall Snap Preview/Apply success and absent/replaced-project refusal, whole-`GroupToken` physical junction materialization/replacement, dedicated owner/dependency/fingerprint persistence, the full L/T/X/Multi ownership matrix, rebuild/cleanup, cross-DWG and host-retention checks, save/reopen and Undo/Redo.
