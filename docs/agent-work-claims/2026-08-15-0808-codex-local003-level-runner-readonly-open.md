# Work claim — LOCAL-003 Level runner read-only open sequence

- Status: `ACTIVE`
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
- Do not mark issue `#1125` or LOCAL-003 complete; they remain open / `PENDING_LOCAL` until `/root` performs a fresh licensed exact-merge-SHA rerun.

## Validation plan

- Windows PowerShell 5.1 parser validation.
- Focused Level runtime-preflight plus the nine established Level/Beam static gates.
- Installed-reference BricsCAD V25 `Release|x64` build with zero warnings/errors.
- Final diff/readback and ordinary PR/merge; hand the exact final `main` SHA to `/root` without dispatching Actions or starting BricsCAD.

## Coordination

The parent LOCAL-003 claim retains licensed runtime execution and evidence. The completed read-only DWG-guard claim owns no remaining implementation. Active P11/LOCAL-004/native-readiness lanes are disjoint and are not modified.

## Completion condition

A merged runner-only launch correction uses an official/noninteractive read-only open sequence while retaining every read-only/hash/cleanup proof, and the exact final `main` SHA is handed to `/root` for the licensed rerun.
