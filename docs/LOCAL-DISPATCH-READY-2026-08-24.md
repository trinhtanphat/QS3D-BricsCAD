# QS3D exact-SHA LOCAL_ONLY dispatch — 2026-08-24

Parent: #72  
Coordination: #3680  
Purpose: give local-capable workers a source-ready queue where they only fetch/checkout, build, run, test, and report evidence. Local workers do not implement production-source fixes.

## Non-negotiable local contract

For every row below:

1. `git fetch --all --prune`.
2. Check out the named pushed carrier at the exact SHA published by its owning issue; never run an approximate moving `latest`.
3. Require a clean tracked worktree before build/runtime execution.
4. Build and run only from that one source/binary identity; never mix evidence from different SHAs.
5. Run the relevant focused preflights/Core smoke/V25 or V26 build before licensed runtime execution.
6. Publish only sanitized `PASS`, `FAIL`, or `NO_RESULT` evidence tied to the exact tested SHA/ProductVersion/plugin identity.
7. If licensed runtime reveals a production-source defect, stop. Do not patch production source in the local lane. Return sanitized reproduction/evidence to a separate remote/source issue and rerun only after a new pushed exact SHA exists.
8. Do not commit BricsCAD proprietary DLLs, private/customer DWGs, credentials, signing material, raw handles/project IDs, or unsanitized runtime dumps.

## P0 — #1744 Slab opening peer replay + Undo semantic coherence

Status: `LOCAL_RERUN_READY`  
Carrier: `agent/control01/slabopen-undo-semantic-1744`  
Exact SHA: `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`

Run the existing #1744 licensed V25.2.10 matrix. The important regression is native Undo restoring both the retiring CAD solid and matching semantic host/opening metadata (`GeneratedSolidHandle`, peer applied handles/fingerprints and `SlabOpeningCutCount`), followed by Health=0, coherent Redo, save/cold-reopen and second-DWG isolation.

The old scheduling dependency on #3593 P06 is obsolete. #3593 reached P07 `LOCAL_PASS` and is closed; #3621 is also closed. Do not rerun H.1 P06/P07 merely because older runbook text still mentions it.

Disposition: PASS => post sanitized evidence to #1744 and #72 and close the bounded child. FAIL => return evidence to a new remote/source fix lane. NO_RESULT => bounded retry only.

## P0 — #3681 StructuralWall live-BREP concrete-contact/formwork

Status: `LOCAL_READY / PULL_RUN_ONLY`  
Original source issue/PR: #3665 / #3666  
Earlier source defects/fixes: #3687 / #3692 and #3697 / #3702  
Touching-contact source defect/fixes: #3711 / #3716 / #3729  
Required source-fix ancestor: `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`  
Runnable carrier: `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh`  
Exact runnable SHA: published on #3681 and #72 after this carrier's protected branch CI succeeds.

The post-#3716 licensed source `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf` is superseded failing evidence: touching-only still failed while 0.05 m penetration remained correct. Licensed stage diagnostics proved BricsCAD V25 rejected the 1 micrometre native OffsetBody probe but accepted 10 micrometres with the correct `0.1600 m²` eligible original-face area. PR #3729 integrated the unit-aware 10 micrometre native-probe floor as `main@4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`. Do not rerun #3681 on `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf` or any older carrier.

Local agents do not author a wall, paste commands, assemble a matrix, edit C#, or modify production source. After fetching/checking out the exact runnable SHA, their complete action is:

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

Disposition: `LOCAL_PASS` => post the sanitized JSON/evidence to #3681 and #72 and close #3681. `LOCAL_FAIL` => return the exact bounded failure to a separate remote/source defect lane. `NO_RESULT` => environment/license/host retry only. No local source coding is authorized or needed.

## P1 — #3613 Coordination Manager Locate through zoom

Status: `LOCAL_READY / PENDING_LOCAL`  
Carrier: `agent/qs3d-uix-worker-b/issue-3613-coordination-locate-zoom`  
Exact SHA: `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`

Use licensed BricsCAD V25 (and V26 parity where applicable). Verify Coordination Manager Locate resolves both sides all-or-nothing, sets exactly the intended PICKFIRST selection, synchronously frames it, preserves exact selection if framing cannot be calculated, refuses stale/missing/wrong-drawing provenance, stays document-affine across modeless active-DWG switches, and leaves no unhandled UI/runtime exception or process/private-state residue.

The former fallback-behind-#3593 wording is obsolete because #3593 is already closed with P07 `LOCAL_PASS`.

Disposition: PASS => post sanitized evidence to #3613 and #72 and close #3613. FAIL => remote/source defect lane. NO_RESULT => bounded retry only.

## Existing broader local queue

This dispatch file does not replace `docs/LOCAL-AGENT-INBOX.md`; it fixes the immediate exact-SHA dispatch ambiguity for the rows above. The broader canonical queue remains governed by #72 and the inbox. Do not rerun already-completed LOCAL-017, LOCAL-018, LOCAL-019, #1744, #3613, or H.1 P07 solely because historical text still mentions them. Prefer remaining P0 work before P1, then P2, and always use the exact pushed carrier declared by the owning issue.