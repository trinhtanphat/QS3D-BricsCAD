# QS3D exact-SHA LOCAL_ONLY dispatch — 2026-08-24

Parent: #72  
Coordination: #3680  
Purpose: give local-capable workers a source-ready queue where they only fetch/checkout, build, run, test, and report evidence. Local workers do not implement production-source fixes.

## Non-negotiable local contract

For every row below:

1. `git fetch --all --prune`.
2. Check out the named pushed carrier branch and verify `git rev-parse HEAD` equals the exact SHA recorded for that row.
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

Status: `LOCAL_READY / PENDING_LOCAL`  
Source issue/PR: #3665 / #3666  
Source-ready carrier: `agent/chatgpt-gpt56sol/issue-3665-wall-contact-brep`  
Exact SHA: `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`

Use licensed BricsCAD V25 to qualify live native BREP contact measurement and quantity refresh. Cover full vertical end-face contact, partial contact, multiple neighboring concrete elements with union/no-double-subtraction behavior, top/bottom exclusion, stale/missing/unresolvable BREP fail-closed behavior, semantic-capture refresh, save/cold-reopen, and Undo/Redo/second-DWG isolation where the mutation path applies.

Required BLT regression control:

- gross formwork: `2.6688 m²`;
- concrete-contact deduction: `0.3200 m²`;
- net formwork: `2.3488 m²`.

Record gross/contact/opening-reveal/net values before and after each relevant case. Local must not modify production source if any case fails.

Disposition: PASS => post sanitized evidence to #3681 and #72 and close #3681. FAIL => new remote/source defect lane. NO_RESULT => bounded retry only.

## P1 — #3613 Coordination Manager Locate through zoom

Status: `LOCAL_READY / PENDING_LOCAL`  
Carrier: `agent/qs3d-uix-worker-b/issue-3613-coordination-locate-zoom`  
Exact SHA: `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`

Use licensed BricsCAD V25 (and V26 parity where applicable). Verify Coordination Manager Locate resolves both sides all-or-nothing, sets exactly the intended PICKFIRST selection, synchronously frames it, preserves exact selection if framing cannot be calculated, refuses stale/missing/wrong-drawing provenance, stays document-affine across modeless active-DWG switches, and leaves no unhandled UI/runtime exception or process/private-state residue.

The former fallback-behind-#3593 wording is obsolete because #3593 is already closed with P07 `LOCAL_PASS`.

Disposition: PASS => post sanitized evidence to #3613 and #72 and close #3613. FAIL => remote/source defect lane. NO_RESULT => bounded retry only.

## Existing broader local queue

This dispatch file does not replace `docs/LOCAL-AGENT-INBOX.md`; it only fixes the immediate exact-SHA dispatch ambiguity for the rows above. The broader canonical queue remains governed by #72 and the inbox. Do not rerun already-completed LOCAL-017, LOCAL-018, or H.1 P07. Prefer remaining P0 work before P1, then P2, and use the exact pushed carrier declared by the owning issue.
