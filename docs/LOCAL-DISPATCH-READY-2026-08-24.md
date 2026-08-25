# QS3D exact-SHA LOCAL_ONLY dispatch — 2026-08-24

Parent: #72  
Coordination: #3680  
Purpose: give local-capable workers a source-ready queue where they only fetch/checkout, build, run, test, and report evidence. Local workers do not implement production-source fixes.

## Non-negotiable local contract

For every active row below:

1. `git fetch --all --prune`.
2. Check out the named pushed carrier at the exact SHA published by its owning issue; never run an approximate moving `latest`.
3. Require a clean tracked worktree before build/runtime execution.
4. Build and run only from that one source/binary identity; never mix evidence from different SHAs.
5. Run the relevant focused preflights/Core smoke/V25 or V26 build before licensed runtime execution.
6. Publish only sanitized `PASS`, `FAIL`, or `NO_RESULT` evidence tied to the exact tested SHA/ProductVersion/plugin identity.
7. If licensed runtime reveals a production-source defect, stop. Do not patch production source in the local lane. Return sanitized reproduction/evidence to a separate remote/source issue and rerun only after a new pushed exact SHA exists.
8. Do not commit BricsCAD proprietary DLLs, private/customer DWGs, credentials, signing material, raw handles/project IDs, or unsanitized runtime dumps.

## Completed bounded references — DO_NOT_RERUN

- **#1744 Slab opening peer replay + Undo semantic coherence:** accepted licensed V25 `LOCAL_PASS` on exact `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`, including peer replay, native Undo/Redo semantic coherence, Health=0, save/cold-reopen and second-DWG isolation. Do not schedule it again from historical queue text.
- **#3613 Coordination Manager Locate/zoom:** accepted licensed V25 `LOCAL_PASS` on exact `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`, including exact PICKFIRST selection, synchronous framing, fail-closed provenance and modeless multi-DWG affinity. Do not schedule it again.
- **#3681 StructuralWall live-BREP concrete-contact/formwork:** accepted licensed V25 `LOCAL_PASS` on exact runtime source `a4f1a53683a9296532a0290fcb79bc49b9d4b892`; sanitized evidence PR #3849 merged as `7fec6f36a7c1181d7113f0e7220ea3dafca66e29`. Do not rerun unless a material source change explicitly reopens qualification.
- **H.1 #3593/#3621:** final P07 is authoritative; obsolete P06 scheduling text must not trigger another run.

## Completed history — #3681 StructuralWall live-BREP concrete-contact/formwork

Status: `COMPLETED / DO_NOT_RERUN`  
Original source issue/PR: #3665 / #3666  
Earlier source defects/fixes: #3687 / #3692 and #3697 / #3702  
Touching-probe floor source defect/fixes: #3711 / #3716 / #3729  
Harness-minimum correction: #3754 / #3833  
Finite touching-footprint correction: #3770 / #3836  
Minimum source-ready ancestor: `c64eb8c1b83761e155da670904a72e64669464b7`  
Exact runtime source: `a4f1a53683a9296532a0290fcb79bc49b9d4b892`  
Accepted evidence: PR #3849 / merge `7fec6f36a7c1181d7113f0e7220ea3dafca66e29`

The post-#3716 licensed source `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf` remains superseded failing evidence: touching-only still failed while 0.05 m penetration remained correct. Licensed stage diagnostics proved BricsCAD V25 rejected the 1 micrometre native OffsetBody probe but accepted 10 micrometres with the correct `0.1600 m²` eligible original-face area. PR #3729 integrated that unit-aware 10 micrometre native-probe floor at `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`. PR #3833 integrated the corrected local fixture minimum, PR #3836 integrated original-footprint authority, and the final licensed matrix passed on `a4f1a53683a9296532a0290fcb79bc49b9d4b892`.

The committed runner `scripts/run-local-v25-wall-contact-3681.ps1` remains only as a regression reference. Do not execute it by default. A future run is authorized only if a material source change explicitly reopens #3681 qualification.

The accepted runner covered the source-fix gate, touching-only one-end at deduction `0.1600 m²`, residual/net `2.5088 m²`, `failed_native=0`, the **0.05 m penetration regression**, partial/union/top-bottom/stale/capture-refresh/two-end, second-process isolation, save/cold-reopen and the BLT `gross 2.6688 - contact 0.3200 = net 2.3488 m²` control. This is historical qualification evidence, not an active dispatch instruction.

## P1 source-ready continuations

- **LOCAL-005 / #83:** source defect #3715 is fixed by merged PR #3727 (`ba6e1c7508086beb8ac5db9a4a78d2c43fc09492`). On one exact descendant, local reruns only multi-region build -> native Undo -> native Redo first; broader refresh/add/remove/corrupt/cap/Foundation/save-reopen/multi-DWG resumes only after coherence passes.
- **LOCAL-006 / #77:** source defect #3721 is fixed by merged PR #3728 (`887173f28126b928765e458f28202e83a6f3b88f`). On one exact descendant, local reruns only `QS3DTAG -> native Undo -> native Redo` first; broader documentation lifecycle/visual matrix resumes only after coherence passes.

## Existing broader local queue

This dispatch file does not replace `docs/LOCAL-AGENT-INBOX.md`; it fixes the immediate exact-SHA/source-first ambiguity for the rows above. The broader canonical queue remains governed by #72 and the inbox. Do not rerun already-completed LOCAL-017, LOCAL-018, LOCAL-019, #1744, #3613, #3681, or H.1 P07 solely because historical text still mentions them. Prefer remaining P0 work before P1, then P2, and always use the exact pushed carrier declared by the owning issue.