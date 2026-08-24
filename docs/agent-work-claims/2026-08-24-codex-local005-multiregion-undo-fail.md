# LOCAL-005 licensed qualification — native multi-region build PASS, Undo semantic divergence

- Status: `LOCAL_FAIL / SOURCE_HANDOFF`
- Parent: `#83` / `LOCAL-005`
- Source defect: `#3715`
- Queue parent: `#72`
- Tested source SHA: `fda4bdf9a34d8f59a01f5e2beb08eea81233d88b`
- Evidence branch: `agent/codex/issue83-local005-v25-qualification`
- Date: 2026-08-24

## Exact runtime identity

- Licensed host: BricsCAD V25.2.10 Windows x64.
- Plugin ProductVersion: `0.1.0-preview.10081`.
- Plugin SHA-256: `A1285CD6AB23C0070B7780FA2CD6BAF0BE83B063EAE8B0BBD76421A34DEE21F1`.
- Core SHA-256: `7C160076CA4FDE746B4755289090D99AD3B7A9DDA455155BB26E06848E12B8B7`.
- Runtime exact-PDB SourceLink checks passed for both plugin and Core.
- The disposable test drawing used native `Millimeter` units.

Before runtime, the LOCAL-005 protected preflight passed, the full Core smoke suite returned `ALL PASS`, and the licensed-reference V25 `Release|x64` build completed with `0 warnings / 0 errors`. The ignored runtime probe also built with `0 warnings / 0 errors`.

## Production build boundary that passed

The BricsCAD command stream selected three real closed POLYLINE sources for one canonical Slab owner and invoked production `QS3DSLABREBAR3DMULTI`:

- two disconnected outer regions;
- one contained hole;
- one outer source with a real bulged segment;
- stable source-region topology: 2 regions / 1 hole;
- generated output: 47 live native `Solid3d` bars;
- source manifest, generated manifest, aggregate handles and counts agreed;
- native owner and per-region owner markers matched;
- read-only multi-region Health returned 0 issues.

This is a bounded positive build result only. It does not qualify the complete LOCAL-005 matrix.

## Native Undo failure

A native `UNDO Mark` was placed immediately before the production build. After production build verification, native `UNDO Back` produced:

| Observation | Result |
| --- | ---: |
| Live generated `Solid3d` bars | 0 |
| Live source POLYLINE loops | 3 |
| Remaining generated semantic property keys | 13 |
| Remaining aggregate generated handles | 47 |
| Multi-region Health issues | 47 |
| Semantic baseline restored | false |

Sanitized failure code: `NATIVE_UNDO_SEMANTIC_DIVERGENCE`.

The native CAD transaction was undone, but the canonical `ProjectState` retained the post-build aggregate handles/manifests/counts and therefore referenced erased native objects. This is a source/runtime defect, not a geometry-planning failure. The local worker stopped before Redo and before the broader refresh/add/remove/corrupt/cap/Foundation/save-reopen/multi-DWG matrix, as required by fail-fast local scope.

## Isolation and cleanup

- DemandLoad `LoadCtrls` was isolated `2 -> 4 -> 2` and the registered loader identity/hash was preserved.
- The launched BricsCAD process exited gracefully; zero BricsCAD processes remained.
- The disposable DWG, sidecar candidates and private command script were removed.
- Raw probe/runner artifacts remain git-ignored and are not part of this commit.
- No private/customer DWG was used.
- No workflow was dispatched and no production source was changed in this local lane.

## Required source handoff and rerun

Issue `#3715` owns the source correction. The multi-region builder must participate in the document-bound native Undo/Redo bridge so one Undo restores both CAD and canonical semantic state without weakening atomic rollback, ownership validation, Health, the 12,000-bar cap or legacy behavior.

After a source fix is merged, rerun the same exact-SHA build -> Undo -> Redo cell first. Only when that cell is coherent should LOCAL-005 resume refresh/add/remove-region reconciliation, corrupt/mixed-owner refusal, cap behavior, Foundation, save/cold-reopen and multi-DWG qualification. Parent `#83` remains open.
