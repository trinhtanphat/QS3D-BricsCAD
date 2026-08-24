# LOCAL-005 licensed qualification — post-#3715 build/Undo/Redo cell PASS

- Status: `LOCAL_PASS` for the bounded post-fix cell; broader `LOCAL-005` remains `OPEN`
- Parent: `#83` / `LOCAL-005`
- Source fix: `#3715`, merged by PR `#3727`
- Queue parent: `#72`
- Tested source SHA: `ba6e1c7508086beb8ac5db9a4a78d2c43fc09492`
- Evidence branch: `agent/codex/issue83-3715fix-v25-local-qualification`
- Date: 2026-08-24

## Exact runtime identity

- Licensed host: BricsCAD V25.2.10 Windows x64.
- Plugin ProductVersion: `0.1.0-preview.10081`.
- Plugin SHA-256: `68E5CF5412D389BA1B4DFFC8606E87D7454CBAEE20E66601B141F3D0CCE89129`.
- Core SHA-256: `6DCF1768664FF2A643BB36E766B4AA0CE81B1187528D1365E34633AF783F47FE`.
- Runtime exact-PDB SourceLink checks passed for both plugin and Core.
- The disposable drawing used native `Millimeter` units.

Before licensed execution, all four relevant source guards passed:

- `preflight-local005-native-multiregion.py`;
- `preflight-local005-multiregion-native-undo-semantic.py`;
- `preflight-polygon-multi-region-topology.py`;
- `preflight-polygon-multi-region-mesh.py`.

The Core `Release|x64` build completed with `0 warnings / 0 errors`, the complete deterministic Core smoke executable returned `ALL PASS`, the licensed-reference V25 `Release|x64` build completed with `0 warnings / 0 errors`, and the gitignored runtime probe built with `0 warnings / 0 errors`.

## Production build state

The production command stream invoked `QS3DSLABREBAR3DMULTI` against three real closed native POLYLINE sources for one canonical Slab owner:

- two disconnected outer regions;
- one contained hole;
- one outer source with a real bulged segment;
- 2 stable generated regions;
- 47 live native `Solid3d` bars;
- matching source/generated manifests, aggregate/per-region ownership and counts;
- read-only multi-region Health: 0 issues.

## Native Undo state

A native `UNDO Mark` preceded the production build and `UNDO Back` reverted it. The post-Undo state was:

| Observation | Result |
| --- | ---: |
| Live generated `Solid3d` bars | 0 |
| Live source POLYLINE loops | 3 |
| Generated semantic property keys | 0 |
| Aggregate generated handles | 0 |
| Multi-region Health issues | 0 |
| Element semantic baseline restored | true |
| Project/version/audit baseline digest restored | true |

Sanitized result: `native_undo_semantic_coherent=true`.

## Native Redo state

The Redo proof followed the repository's established two-cycle native pattern so no probe command was inserted between the grouped native `U` and `REDO`. `UNDO Begin` clears PICKFIRST in this host, so the same three source POLYLINEs were deliberately reselected after the group began and before the second production build. The second healthy build was captured, grouped, undone with native `U`, and immediately restored with native `REDO`.

The post-Redo state was:

| Observation | Result |
| --- | ---: |
| Live generated `Solid3d` bars | 47 |
| Generated semantic property keys | 13 |
| Multi-region Health issues | 0 |
| Generated handle set restored | true |
| Project/version/audit digest restored | true |
| Native handle/extents/volume digest restored | true |
| Generated semantic property shape restored | true |

Sanitized results: `native_redo_semantic_coherent=true` and `bounded_build_undo_redo_cell_qualified=true`.

## Isolation and cleanup

- DemandLoad `LoadCtrls` was isolated `2 -> 4 -> 2` and the registered loader identity/hash was preserved.
- BricsCAD exited gracefully; zero BricsCAD processes remained.
- The disposable DWG, native lock/backup files, sidecar candidates and private command script were removed; the fixture directory was empty and removed.
- Raw runner/probe/trace artifacts remain gitignored and are not part of this commit.
- No private/customer DWG was used.
- No GitHub Actions workflow was dispatched and no production source was changed in this local lane.

## Scope boundary

This PASS retires the exact post-`#3715` build -> native Undo -> native Redo blocker and supports keeping source issue `#3715` closed. It does not close parent `#83` or claim complete `LOCAL-005` qualification. The remaining refresh/add/remove-region reconciliation, corrupt/mixed-owner refusal, cap behavior, Foundation, save/cold-reopen and multi-DWG cells remain pending and must be run only through approved exact runners/resources.
