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
- **H.1 #3593/#3621:** final P07 is authoritative; obsolete P06 scheduling text must not trigger another run.

## P0 — #3681 StructuralWall live-BREP concrete-contact/formwork

- Status: `SOURCE_HARNESS_DEFECT / BLOCKED_SOURCE_HARNESS_FIX / NO PRODUCT VERDICT`
- Original source issue/PR: #3665 / #3666
- Earlier source defects/fixes: #3687 / #3692 and #3697 / #3702
- Touching-contact source defect/fixes: #3711 / #3716 / #3729
- Required source-fix ancestor: `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`
- Superseded runnable carrier: `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh`
- Tested exact SHA: `881f7b57176514e6e87c943f88165a5868c68539`
- Next runnable SHA: pending merged harness correction #3754.

The post-#3716 licensed source `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf` is superseded failing evidence: touching-only still failed while 0.05 m penetration remained correct. Licensed stage diagnostics proved BricsCAD V25 rejected the 1 micrometre native OffsetBody probe but accepted 10 micrometres with the correct `0.1600 m²` eligible original-face area. PR #3729 integrated the unit-aware 10 micrometre native-probe floor as `main@4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`. Do not rerun #3681 on `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf` or any older carrier.

The subsequent official run on exact clean `main@881f7b57176514e6e87c943f88165a5868c68539` reached the native source-fix gate and returned `LOCAL_FAIL` at `touching_one_end_deduction`. A separate licensed bounds probe showed the two committed harness helpers misinterpret the centered V25 `Solid3d.CreateBox` origin as a requested minimum corner, converting the intended touching fixture into overlap. This is a source-safe harness defect, not a product verdict. #3754 owns the helper correction and placement guard; keep #3681 open and do not rerun the unchanged harness.

Local agents do not author a wall, paste commands, assemble a matrix, edit C#, or modify production source. Only after #3754 merges and a new exact corrected SHA is published, their complete action is:

```powershell
.\scripts\run-local-v25-wall-contact-3681.ps1
```

The runner automatically:

- requires Windows, an interactive licensed V25 session, a clean worktree, and the merged #3729 source-fix ancestor `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`;
- runs repository preflights, Core smoke and V25 Release|x64 build through the committed baseline runner;
- builds `tests/QS3D.BricsCAD.V25.LocalQualification`, including a focused source-fix gate plus the broader local-only x64/net48 qualification harness; both invoke production contact/capture/context paths;
- **fails fast before the broader matrix** unless touching-only one-end passes at deduction `0.1600 m²`, residual/net `2.5088 m²`, `failed_native=0`, through the production contact-probe path;
- immediately proves the **0.05 m penetration regression** still passes at deduction `0.1600 m²`, residual/net `2.5088 m²`, `failed_native=0`, through the positive-volume path;
- only after both mandatory cells pass, proves baseline, partial `0.0800 m²`, overlapping-neighbor union `0.1600 m²`, top/bottom exclusion, stale/missing BREP clearing and semantic-capture refresh;
- proves the contact measurement is read-only, so native Undo/Redo is explicitly not applicable to that read-only measurement path rather than being faked;
- repeats the deterministic broader geometry matrix in a second fresh BricsCAD process/drawing for isolation;
- creates a test-owned scratch DWG + QSDB with the two-end contact, saves, closes, cold-reopens, remeasures through production code and removes the scratch DWG/QSDB afterwards;
- requires the BLT control `gross 2.6688 - contact 0.3200 = net 2.3488 m²` after cold reopen;
- records exact Git SHA, source-fix ancestor, plugin/Core ProductVersion + SHA-256, harness hash and sanitized case status under ignored `artifacts/local-v25-wall-contact-3681/`;
- exits `0` only for `LOCAL_PASS`, `1` for `LOCAL_FAIL`, and `2` for `NO_RESULT`.

Current disposition: `SOURCE_HARNESS_DEFECT / LOCAL_FAIL / NO PRODUCT VERDICT`; source lane #3754 must merge before rerun. On the corrected exact SHA: `LOCAL_PASS` => post the sanitized JSON/evidence to #3681 and #72 and close #3681; `LOCAL_FAIL` => return the exact bounded failure to a separate remote/source defect lane; `NO_RESULT` => environment/license/host retry only. No local source coding is authorized or needed.

## P1 source-ready continuations

- **LOCAL-005 / #83:** source defect #3715 is fixed by merged PR #3727 (`ba6e1c7508086beb8ac5db9a4a78d2c43fc09492`). On one exact descendant, local reruns only multi-region build -> native Undo -> native Redo first; broader refresh/add/remove/corrupt/cap/Foundation/save-reopen/multi-DWG resumes only after coherence passes.
- **LOCAL-006 / #77:** source defect #3721 is fixed by merged PR #3728 (`887173f28126b928765e458f28202e83a6f3b88f`). On one exact descendant, local reruns only `QS3DTAG -> native Undo -> native Redo` first; broader documentation lifecycle/visual matrix resumes only after coherence passes.

## Existing broader local queue

This dispatch file does not replace `docs/LOCAL-AGENT-INBOX.md`; it fixes the immediate exact-SHA/source-first ambiguity for the rows above. The broader canonical queue remains governed by #72 and the inbox. Do not rerun already-completed LOCAL-017, LOCAL-018, LOCAL-019, #1744, #3613, or H.1 P07 solely because historical text still mentions them. Prefer remaining P0 work before P1, then P2, and always use the exact pushed carrier declared by the owning issue.
