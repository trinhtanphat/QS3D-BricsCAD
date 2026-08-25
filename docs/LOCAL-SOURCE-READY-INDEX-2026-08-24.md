# LOCAL_ONLY source-ready index — 2026-08-24

Status: `SOURCE_READY / LOCAL_RUN_ONLY`

Parent: #72

Lane-Key: `issue-3680`

Canonical coordination branch: `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh`

Latest integrated source baseline for this sweep: `c64eb8c1b83761e155da670904a72e64669464b7`

This index records the owner-requested boundary: remote/source work is prepared and pushed first; a compatible local agent should only fetch an exact published carrier, build/run the licensed BricsCAD matrix, and publish sanitized `PASS` / `FAIL` / `NO_RESULT`. A local runtime defect that requires production-source correction returns to a bounded remote/source lane. Local workers must not opportunistically patch normal production source.

This file does **not** manufacture `LOCAL_PASS`. Runtime status remains governed by #72, `docs/LOCAL-AGENT-INBOX.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-V26-QUALIFICATION.md`, and the linked per-feature runbooks.

## Immediate source-ready dispatch

Completed historical rows are not rescheduled here. #1744, #3613 and #3681 already have accepted licensed bounded PASS evidence and must not be rerun merely because this index changed. LOCAL-005 post-#3715 build -> native Undo -> native Redo and LOCAL-006 post-#3721 `QS3DTAG -> native Undo -> native Redo` also have accepted licensed bounded evidence and are not first-step rerun targets anymore.

| Priority | Issue / row | Accepted bounded evidence | Local action |
| --- | --- | --- | --- |
| P1 | LOCAL-005 / #83 | `LOCAL_PASS` on exact `main@ba6e1c7508086beb8ac5db9a4a78d2c43fc09492`; sanitized evidence PR #3735 merged as `73fec2c48726c09196b773c117be77ee1983031e` | Continue only the still-pending refresh/add/remove-region, corrupt/mixed-owner refusal, cap, Foundation, save/cold-reopen and multi-DWG cells on one exact intended descendant. Do not repeat the accepted build -> native Undo -> native Redo cell solely because this index changed. |
| P1 | LOCAL-006 / #77 | `BOUNDED_LOCAL_PASS / OVERALL_IN_PROGRESS` on exact tested source `a572ab0a350f54f8e994ac1e91f825907646af9c`; sanitized evidence PR #3777 merged as `7f30d019a97d36c025c34a4e08364ef3bd73ffad` | Continue only the broader MLeader/Table/Sheet/Layout/Viewport/Unicode/HiDPI/save-reopen/multi-DWG/V26 matrix on one exact intended descendant. Do not repeat the accepted `QS3DTAG -> native Undo -> native Redo` cell solely because this index changed. |

## Completed #3681 licensed wall-contact qualification — DO_NOT_RERUN

#3681 StructuralWall live-BREP concrete-contact/formwork is complete. Licensed BricsCAD V25 qualification passed on exact runtime source `a4f1a53683a9296532a0290fcb79bc49b9d4b892`; sanitized evidence was merged through PR #3849 as `7fec6f36a7c1181d7113f0e7220ea3dafca66e29`. The accepted source contains minimum source-ready ancestor `c64eb8c1b83761e155da670904a72e64669464b7` (#3833 + #3836) and touching-probe floor `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0` (#3729).

The committed regression runner remains `scripts/run-local-v25-wall-contact-3681.ps1`, but #3681 is `COMPLETED / DO_NOT_RERUN`. Do not schedule or execute it again unless a material source change explicitly reopens the qualification. Historical failing carriers, including `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf`, remain diagnostic provenance only and are not runnable candidates.

The accepted licensed matrix covered the source-fix gate, touching-only one-end deduction `0.1600 m²`, residual/net `2.5088 m²`, `failed_native=0`, the **0.05 m penetration** regression at the same deduction/residual, partial/union/top-bottom/stale/capture-refresh/two-end, second-process isolation, save/cold-reopen and the BLT `2.6688 - 0.3200 = 2.3488 m²` control. This historical description does not authorize another local run.

Do not rerun the obsolete #3593 P06 binary. The later #3593 P07 result and closed #3621 source lane are authoritative for that H.1 correction.

LOCAL-019 is already complete, not pending dispatch: PR #3693 merged the six-sheet QS Review export + Excel-to-Model Locate source, and licensed BricsCAD V25/V26 qualification passed on exact runtime-tested source SHA `9cfff87262d7a7117c5ef1f03b486271a0723fa3`. Do not schedule a repeat solely because this source-ready index was refreshed.

## All canonical LOCAL_ONLY rows

| Local row | Priority / status | Remote/source disposition | What remains for the local agent |
| --- | --- | --- | --- |
| LOCAL-001 | P0 / PASS | Source, baseline runner and lifecycle probes are integrated. | Only explicitly unqualified wider interactive/private/release cells; start from `scripts/run-local-v25-qualification.ps1`. |
| LOCAL-002 | P0 / OPEN | `REMOTE_DONE`; Curtain host/frame/panel atomicity, ownership, Health, Family writes and P01-P12 probes exist. H.1 source fix is integrated; stale P06 text is historical only. | Licensed broad H.1/final runtime matrix only. Do not re-code Curtain or #3621 locally. |
| LOCAL-003 | P0 / IN_PROGRESS | `SOURCE_INTEGRATED / AUTOMATED_RUNTIME_PROBE_PASS`; shared Level Z-chain is implemented. | Full licensed dual-unit/Undo/save-reopen/multi-DWG/private matrix only. |
| LOCAL-004 | P0 / IN_PROGRESS | Source Reconcile implementation plus P01-P05 native runners/probes are integrated and bounded cells passed. | Broader #80 topology/category/dependent/failure matrix only. |
| LOCAL-005 | P1 / OPEN | #3715 licensed Undo divergence returned to remote source; PR #3727 merged as `ba6e1c7508086beb8ac5db9a4a78d2c43fc09492`. The corrected bounded production build -> native Undo -> native Redo cell then passed licensed V25 on that exact SHA; sanitized evidence PR #3735 merged as `73fec2c48726c09196b773c117be77ee1983031e`. | Preserve that accepted bounded PASS. Continue only refresh/add/remove-region, corrupt/mixed-owner refusal, cap, Foundation, save/cold-reopen and multi-DWG on an exact intended descendant. |
| LOCAL-006 | P1 / OPEN | #3721 licensed Semantic Tag Undo divergence returned to remote source; PR #3728 merged as `887173f28126b928765e458f28202e83a6f3b88f`. The bounded `QS3DTAG -> native Undo -> native Redo` rerun then passed on exact tested source `a572ab0a350f54f8e994ac1e91f825907646af9c`; sanitized evidence PR #3777 merged as `7f30d019a97d36c025c34a4e08364ef3bd73ffad`. | Preserve that accepted bounded PASS. Continue only MLeader/Table/Sheet/Layout/Viewport/Unicode/HiDPI/save-reopen/multi-DWG/V26 runtime coverage on an exact intended descendant. |
| LOCAL-007 | P1 / OPEN parent | Bounded P01/P02/P03 implementation is integrated. PR #3616 is merged (`12b5f0d7d8549d8b107a1b921d2bb431f809bf69`), so “P03 not integrated” is stale history. | Only parent #73 advanced geometry outside the qualified P01/P02/P03 boundary. |
| LOCAL-008 | P1 / OPEN | `REMOTE_DONE` for quick/advanced split, preview-project/context freshness, Window/Auto Host safeguards and repeated mode source. | Licensed prompt-cancel/drift/Auto Host/reference/Ribbon/UI remainder only. |
| LOCAL-009 | P1 / OPEN | Packaging/install/update source tooling exists; unsigned preview/DemandLoad bounded evidence exists. | Real clean-machine rollback/uninstall, SECURELOAD and approved signing/trust evidence. No signing key belongs in Git. |
| LOCAL-010 | P2 / OPEN | No repository-safe production gap is known. | Hardware/GPU/large-model timing and DPI/layout measurements only. |
| LOCAL-011 | P1 / OPEN | `REMOTE_DONE` for Grid/Curtain/Rebar exact-set ownership and rollback guards. | Native transaction/Undo/modeless failure-injection/save-reopen/multi-DWG runtime matrix only. |
| LOCAL-012 | P1 / IN_PROGRESS | `REMOTE_DONE` for Workspace selection bridge boundaries, live Instance reset, #1760 subtype flow and #2399 dedicated QS3D Properties palette. | Real implied selection/live handle/zoom/modeless/document switch/HiDPI qualification only. |
| LOCAL-013 | P0 / IN_PROGRESS | Eligible-CAD B4D -> ED2 -> Excel Locate source/probes are implemented and bounded runtime passed; Recognition apply atomicity is integrated. | Authorized historical/current BRC public-proxy input is still required for BRC parity; do not reconstruct missing private reference data. |
| LOCAL-014 | P1 / OPEN | `REMOTE_DONE` for Plan-to-3D quick/advanced split, source/project freshness and ownership-scoped batch compensation. | Advanced prompt drift/cancel, injected native rollback, Undo/Redo and save/reopen runtime remainder only. |
| LOCAL-015 | P2 / OPEN | `REMOTE_DONE` for document-bound reference search, HTTPS mapping, encoding, SafeSearch and no-scrape boundary. | Windows default-browser/modeless/document-switch behavior only. |
| LOCAL-016 | P0 / IN_PROGRESS | V26 net8 host/build/runtime source plus bounded native authoring/dependent/repeated LINE workflows are integrated. | Remaining #1462 private-DWG/clean-machine/package/signing/release matrix only. |
| LOCAL-017 | P0 / PASS | Bounded V26 native Slab POLYLINE lifecycle is complete. | No rerun unless its source scenario materially changes. |
| LOCAL-018 | P0 / PASS | Exact V26 LINE and repeated Direct Draw lifecycle is complete. | No rerun unless its source scenario materially changes. |
| LOCAL-019 | P0 / PASS | Six-sheet QS Review export + Excel-to-Model Locate source landed through PR #3693; licensed V25/V26 exact-SHA qualification passed on `9cfff87262d7a7117c5ef1f03b486271a0723fa3`. | No rerun unless this scenario materially changes; the committed round-trip runner remains available for regression. |

## Local pull/run contract

For a pinned carrier, local agents use the exact branch and SHA, never an approximate “latest” branch:

```powershell
git fetch origin
git checkout --detach <exact-runtime-carrier-sha>
git status --short
```

The worktree must be clean before qualification. Completed #3681 must not be scheduled from this section; its committed runner remains available only as a regression reference if a material source change explicitly reopens qualification. The accepted LOCAL-005 post-#3715 build -> Undo -> Redo cell and LOCAL-006 post-#3721 `QS3DTAG -> Undo -> Redo` cell must likewise not be repeated solely because the queue was refreshed. Other V25 rows may use the general `scripts/run-local-v25-qualification.ps1` plus the row-specific committed runner/runbook. V26 rows follow `docs/LOCAL-V26-QUALIFICATION.md` and must use the matching V26 `net8.0-windows` plugin, not a relabeled V25 binary.

## Fail/return routing

A local result is one of:

- `PASS`: sanitized evidence tied to exact source SHA/plugin identity;
- `FAIL`: reproducible product/runtime failure with sanitized stage/code and enough bounded evidence for a new/existing remote source lane;
- `NO_RESULT`: environment/license/host-start/operator-session/fixture blocker; no product failure inferred.

A `FAIL` never authorizes a local worker to absorb normal source-safe production work. The source correction is implemented and pushed remotely, then the same local matrix is rerun against the new exact SHA.

## Repository/privacy boundary

Never commit BricsCAD proprietary DLLs, license files, activation material, signing keys, customer/private DWGs, raw handles/ProjectIds/fingerprints, crash dumps, private screenshots, browser history, or unsanitized runtime logs. Keep such runtime material under ignored local artifacts and publish only the sanitized evidence required by the row/runbook.