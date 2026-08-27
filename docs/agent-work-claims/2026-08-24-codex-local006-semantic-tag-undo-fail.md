# LOCAL-006 licensed qualification — Semantic Tag cancel/build PASS, Undo semantic divergence

- Status: `LOCAL_FAIL / SOURCE_HANDOFF`
- Parent: `#77` / `LOCAL-006`
- Source defect: `#3721`
- Queue parent: `#72`
- Tested source SHA: `cfa9b248213dfea7a3621ffc020ffbc7b9a900a2`
- Evidence branch: `agent/codex/issue77-local006-v25-qualification`
- Date: 2026-08-24

## Exact runtime identity

- Licensed host: BricsCAD V25.2.10 Windows x64.
- Plugin ProductVersion: `0.1.0-preview.10081`.
- Plugin SHA-256: `ECC7CD21A673FD5012AEEBA715EA8B20843E33E7D1278CC11262CD6E617A050`.
- Core SHA-256: `3E768BF3BE65813C40AA9C0339B9737B748C4725AFDF7375A2FDD4B1B89ACBB8`.
- Runtime exact-PDB SourceLink checks passed for both plugin and Core.

The official V25 baseline passed on the same exact source SHA: all `1012/1012` aggregate preflights, Core `Release` and V25 `Release|x64` builds with `0 warnings / 0 errors`, Core deterministic smoke `ALL PASS`, offline WPF, and licensed exact-DLL `NETLOAD`/Ribbon/Palette. The ignored LOCAL-006 runtime probe also built with `0 warnings / 0 errors`.

## Semantic Tag boundaries that passed

The licensed command stream exercised production `QS3DTAG` against a disposable cold-cache drawing:

- source-selection cancel returned with no project bind/create/cache, no sidecar, no native object and no semantic/audit mutation;
- placement-point cancel left semantic properties, audit, `ChangeVersion` and native MText count unchanged;
- successful cold-cache placement rebound the canonical same-ProjectId project, created exactly one native MText, advanced audit and `ChangeVersion` exactly once and reported runtime Health 0.

The fully automated final run used one exact-host COM ESC fallback for source-selection cancellation after physical-key injection did not advance the native prompt. Placement cancellation and successful placement proceeded through the licensed native host command flow. The earlier script/input-delivery attempts were harness-only and are not product failures.

## Native Undo failure

A native `UNDO Mark` was placed immediately before the successful production placement. After placement verification, native `UNDO Back` produced:

| Observation | Result |
| --- | ---: |
| Live generated native MText | 0 |
| Native object-count baseline restored | true |
| Generated tag properties baseline restored | false |
| Audit baseline restored | false |
| `ChangeVersion` baseline restored | false |
| Runtime Health issues | 1 |

Sanitized failure code: `NATIVE_UNDO_SEMANTIC_DIVERGENCE`.

The native CAD transaction was undone, but canonical `ProjectState` retained the generated-tag ownership and post-placement audit/version mutation. Redo was therefore `NOT_RUN_FAIL_FAST`. The local worker stopped before the stale-project, refresh/remove, MLeader, Table/custom-schedule, detached review/export, Sheet/Layout/Viewport, save/reopen, multi-DWG, Unicode/HiDPI and V26 cells required by the broader LOCAL-006 matrix.

## Isolation and cleanup

- DemandLoad `LoadCtrls` was isolated `2 -> 4 -> 2` and the registered loader identity/hash was preserved.
- The disposable host process was terminated cleanly by the runner after graceful close did not complete; zero BricsCAD processes remained.
- Disposable DWG/sidecar candidates and the private command script were removed.
- Raw probe/runner artifacts remain git-ignored and are not part of this commit.
- No private/customer DWG was used.
- No workflow was dispatched and no production source was changed in this local lane.

## Required source handoff and rerun

Issue `#3721` owns the source correction. Semantic Tag placement must participate in the document-bound native Undo/Redo bridge so one native Undo restores both CAD and canonical generated-tag ownership, audit and `ChangeVersion` state.

After a source fix is merged, rerun the same exact-SHA cancel -> cold-cache placement -> Undo -> Redo cell first. Only when native and semantic state remain coherent should LOCAL-006 resume the remaining MText/MLeader, Table/custom-schedule, detached review/export, Sheet/Layout/Viewport/title-block, save/cold-reopen, multi-DWG and V26 qualification. Parent `#77` remains open.
