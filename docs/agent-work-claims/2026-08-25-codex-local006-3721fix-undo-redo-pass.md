# LOCAL-006 licensed qualification — post-#3721 Semantic Tag Undo/Redo bounded PASS

- Status: `BOUNDED_LOCAL_PASS / OVERALL_IN_PROGRESS`
- Lane-Key: `issue-77`
- Parent: `#77` / `LOCAL-006`
- Queue parent: `#72`
- Source defect/fix: #3721 / merged PR #3728 (`887173f28126b928765e458f28202e83a6f3b88f`)
- Tested exact Git SHA: `a572ab0a350f54f8e994ac1e91f825907646af9c`
- Evidence branch: `agent/codex/issue77-local006-v25-qualification`
- Host: licensed BricsCAD V25.2.10 Windows x64
- Plugin/Core ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `738F15F13A74621EBED37A0734174507703D4C42F3640834425C82C6627AD283`
- Core SHA-256: `49973A4DAAB0AE4E5A6CD1F3C2D93E5C225BEC32F4C6AAE3978C4D4BE96184FE`

## Exact baseline

The tested branch SHA was pushed before execution and contains the #3728 source fix. The pinned V25 entrypoint verified the same exact SHA before and after qualification. The accepted final baseline passed:

- aggregate feature preflights: `1020/1020`;
- Core Release and V25 `Release|x64`: `0 warnings / 0 errors`;
- Core deterministic smoke: `ALL PASS`;
- offline WPF theme/Workspace/RightPanel checks: PASS;
- licensed exact-candidate `NETLOAD`, V25 host-major identity, Ribbon and Palette runtime probe: PASS;
- plugin and Core PDB SourceLink identity: exact tested SHA.

An earlier baseline attempt correctly rejected the installed AppData payload as a stale runtime marker. The accepted run disabled the installed registration only for the exact manual-NETLOAD window (`LoadCtrls 2 -> 0 -> 2`) and preserved the registered loader path and bytes. No report was edited or combined by hand.

## Bounded production workflow result

The ignored runtime probe drove production `QS3DTAG` in licensed V25 and returned `status=PASS`, `failure_code=NONE`:

- source-selection cancel: no project bind/create/cache or mutation;
- placement cancel: no native/semantic/audit mutation;
- cold-cache placement: canonical ProjectId continuity, exactly one owned native MText, audit delta 1, `ChangeVersion` delta 1 and Health 0.

Native Undo then restored the complete pre-tag state:

| Undo observation | Result |
| --- | --- |
| Live generated MText | `0` |
| Generated-tag properties baseline | restored |
| Audit baseline | restored |
| `ChangeVersion` baseline | restored |
| Native object-count baseline | restored |
| Runtime Health issues | `0` |

Native Redo restored the complete accepted post-build state:

| Redo observation | Result |
| --- | --- |
| Live generated MText | `1` |
| Generated-tag property snapshot | restored |
| Audit snapshot | restored |
| `ChangeVersion` snapshot | restored |
| Native object count and geometry | restored |
| Runtime Health issues | `0` |

The marker explicitly reports `native_undo_semantic_coherent=true`, `native_redo_semantic_coherent=true`, and `bounded_tag_undo_redo_cell_qualified=true`.

## Cleanup, caveat and remaining scope

DemandLoad was restored `2 -> 0 -> 2`, the installed loader path/hash was preserved, the disposable DWG/sidecar/script were removed, and zero BricsCAD processes remained. The host did not complete graceful exit within the runner's 30-second window (`gracefulExit=false`); the harness force-closed only its exact owned PID and verified cleanup. Therefore this evidence does not qualify host shutdown, save/cold-reopen or any broader lifecycle cell.

The marker also explicitly reports `production_local006_qualified=false`. Overall LOCAL-006 and #77 remain OPEN/IN_PROGRESS for MLeader, refresh/remove, native Tables/custom schedules, Sheet/Layout/PaperSpace/Viewport/title blocks, Unicode/HiDPI, save/cold-reopen, multi-DWG and representative V26 parity. No production source, proprietary binary, customer/private DWG, raw handle, ProjectId or sidecar is committed.
