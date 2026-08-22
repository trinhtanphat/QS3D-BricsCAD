# Work claim — LOCAL-003 Level runner read-only open sequence

- Status: `COMPLETED` (implementation handed to the existing owner of overlapping PR `#1432`)
- Agent: `/root/local003_readonly_guard`
- Registered: `2026-08-15T08:08:00+07:00`
- Baseline main SHA: `a8c0632c13dcece41c8d407582397fe29bc0b405`
- Priority: `LOCAL-003 / P0`; parent-authorized automation-only follow-up after exact-SHA `e134219e8ac9e1337c25ae0676ab41d0ee36bc6e` returned `NO_RESULT`

## Confirmed runtime boundary

The guarded exact-SHA run kept the disposable DWG `Archive, ReadOnly`, but BricsCAD remained responsive at `BricsCAD Ultimate - [Start]` for 900 seconds and emitted no probe marker. Cleanup reached zero BricsCAD processes, removed the private script/sidecar/backup/locks, restored the fixture hash exactly, and restored the original `Archive` attribute. This is a runner launch/open-sequence failure, not a product verdict.

## Reserved scope

- Diagnose and implement the smallest official/noninteractive read-only opening sequence in `scripts/test-bricscad-v25-level-z.ps1` that reliably opens the guarded disposable DWG and executes its private `/B` script.
- Extend only `scripts/preflight-level-z-runtime-probe.py` for the exact launch/order contract required by that correction.
- Update this claim for closeout.
- `scripts/bricscad-runner-window-interop.ps1` may be inspected as read-only context; any edit requires a separately published claim expansion before the write.

The correction must preserve exact-SHA/worktree/assembly guards, the zero-process preflight, hidden test-owned launch, the OS read-only attribute before launch and through host exit, the hash comparison before any restore, idempotent backup/attribute/private-state cleanup, sanitized marker evidence and graceful no-save exit.

## Excluded scope

- No production probe, builder, Core, adapter, marker schema, shared interop helper, profile mutation, release workflow or GitHub Actions change.
- Do not launch or interact with BricsCAD and do not use private drawing data.
- Do not reopen historical issue `#1125`; it was closed on 2026-08-14. LOCAL-003 remains `PENDING_LOCAL` until `/root` performs a fresh licensed exact-merge-SHA rerun.

## Validation plan

- Windows PowerShell 5.1 parser validation.
- Focused Level runtime-preflight plus the nine established Level/Beam static gates.
- Installed-reference BricsCAD V25 `Release|x64` build with zero warnings/errors.
- Final diff/readback and ordinary PR/merge; hand the exact final `main` SHA to `/root` without dispatching Actions or starting BricsCAD.

## Coordination

The parent LOCAL-003 claim retains licensed runtime execution and evidence. The completed read-only DWG-guard claim owns no remaining implementation. Active P11/LOCAL-004/native-readiness lanes are disjoint and are not modified.

## Handoff closeout

- Diagnosis: the runner supplied both a positional DWG and a `/B` script at startup, while the responsive host remained on `[Start]` and never emitted the marker. The bounded correction starts BricsCAD with the documented `/Automation` batch switch and has the private script execute `FILEDIA=0` followed by `_.OPEN` for the already OS-read-only disposable drawing before `NETLOAD` and the probe command.
- Prepared implementation commit: `05f57301f45ec1819b9748fed23101b6aa8d92f1`. It changes only `scripts/test-bricscad-v25-level-z.ps1` and `scripts/preflight-level-z-runtime-probe.py`; the read-only-through-host-exit check, pre-restore hash comparison and idempotent attribute/backup restoration remain intact.
- Collision audit found draft PR `#1432` already owns and modifies both files for the separate Millimeter/Meter runtime matrix. No overlapping implementation PR was opened. `/root` posted the exact cherry-pick handoff to `#1432` at `https://github.com/trinhtanphat/QS3D-BricsCAD/pull/1432#issuecomment-5299751090` for integration by that owner.
- Validation on the handed-off commit: Windows PowerShell 5.1 parser `PASS`; the nine established Level/Beam focused gates `PASS`; installed-reference BricsCAD V25 `Release|x64` build `PASS` with zero warnings and zero errors.
- No BricsCAD process or GitHub Actions workflow was started, and no private drawing or evidence data was used. This claim closes only the duplicate implementation lane; it records no runtime verdict. LOCAL-003 remains `PENDING_LOCAL`, and open issue `#1431` plus draft PR `#1432` retain the licensed Millimeter/Meter qualification work.

## Completion condition

The bounded correction and its validation are handed to the existing overlapping PR owner without a competing PR or scope overwrite. Runtime completion remains owned by `#1431` / `#1432`; only their exact merged SHA may be used for the next licensed rerun.
