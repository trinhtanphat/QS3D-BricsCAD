# LOCAL_ONLY source-ready index — 2026-08-24

Status: `SOURCE_READY / LOCAL_RUN_ONLY`

Parent: #72

Lane-Key: `issue-3680`

Canonical coordination branch: `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh`

Latest integrated source baseline for this sweep: `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`

This index records the owner-requested boundary: remote/source work is prepared and pushed first; a compatible local agent should only fetch an exact published carrier, build/run the licensed BricsCAD matrix, and publish sanitized `PASS` / `FAIL` / `NO_RESULT`. A local runtime defect that requires production-source correction returns to a bounded remote/source lane. Local workers must not opportunistically patch normal production source.

This file does **not** manufacture `LOCAL_PASS`. Runtime status remains governed by #72, `docs/LOCAL-AGENT-INBOX.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-V26-QUALIFICATION.md`, and the linked per-feature runbooks.

## Immediate source-ready dispatch

Completed historical rows are not rescheduled here. #1744 and #3613 already have accepted licensed bounded PASS evidence and must not be rerun merely because this index changed.

| Priority | Issue / row | Exact carrier | Minimum integrated source | Local action |
| --- | --- | --- | --- | --- |
| P0 | #3681 | tested `main@881f7b57176514e6e87c943f88165a5868c68539`; next exact carrier pending #3754 | must contain `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0` (#3729) plus the merged #3754 harness correction | `BLOCKED_SOURCE_HARNESS_FIX`; do not rerun the unchanged harness |
| P1 | LOCAL-005 / #83 | exact intended descendant published by #83/#72 | contains `ba6e1c7508086beb8ac5db9a4a78d2c43fc09492` (#3727); current integrated descendant is `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0` | rerun bounded multi-region build -> native Undo -> native Redo first |
| P1 | LOCAL-006 / #77 | exact intended descendant published by #77/#72 | contains `887173f28126b928765e458f28202e83a6f3b88f` (#3728); current integrated descendant is `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0` | rerun bounded `QS3DTAG -> native Undo -> native Redo` first |

For #3681, post-#3716 source `a4ec7cdc84cc63cb35d1162b1e469638ed796ddf` is superseded licensed failing evidence: touching-only still failed while the 0.05 m penetration control passed. Licensed native-stage evidence proved the remaining failure was `OffsetBody(1e-6)` in BricsCAD V25; `1e-5` and larger bounded probes passed with the correct `0.1600 m²` eligible original-face area. PR #3729 integrated the unit-aware 10 micrometre native touching-probe floor as `main@4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0`.

The later official exact-main run on `881f7b57176514e6e87c943f88165a5868c68539` returned `LOCAL_FAIL` at `touching_one_end_deduction`, but a licensed native bounds probe proved that both committed harness helpers place centered `Solid3d.CreateBox` instances as though the requested coordinates were minimum corners. The resulting official geometry is not the intended touching topology. This is `SOURCE_HARNESS_DEFECT / NO PRODUCT VERDICT`; #3754 owns the correction and focused placement guard. #3681 stays open, and local must not rerun until a new merged exact SHA containing #3754 is published.

After #3754 is merged and its new exact descendant is published, the #3681 local action remains deliberately one command after exact checkout:

```powershell
.\scripts\run-local-v25-wall-contact-3681.ps1
```

The runner first executes a **source-fix gate**. It must pass touching-only one-end at deduction `0.1600 m²`, residual/net `2.5088 m²`, `failed_native=0` through the contact-probe path, then immediately pass the **0.05 m penetration** regression at the same deduction/residual through the positive-volume path. Only after both pass does it execute partial/union/top-bottom/stale/capture-refresh/two-end, second-process isolation and save/cold-reopen. It checks the BLT `2.6688 - 0.3200 = 2.3488 m²` control, records plugin/Core identity and removes test-owned scratch state. No manual geometry authoring or production-source patch is delegated to the local agent.

Do not rerun the obsolete #3593 P06 binary. The later #3593 P07 result and closed #3621 source lane are authoritative for that H.1 correction.

LOCAL-019 is already complete, not pending dispatch: PR #3693 merged the six-sheet QS Review export + Excel-to-Model Locate source, and licensed BricsCAD V25/V26 qualification passed on exact runtime-tested source SHA `9cfff87262d7a7117c5ef1f03b486271a0723fa3`. Do not schedule a repeat solely because this source-ready index was refreshed.

## All canonical LOCAL_ONLY rows

| Local row | Priority / status | Remote/source disposition | What remains for the local agent |
| --- | --- | --- | --- |
| LOCAL-001 | P0 / PASS | Source, baseline runner and lifecycle probes are integrated. | Only explicitly unqualified wider interactive/private/release cells; start from `scripts/run-local-v25-qualification.ps1`. |
| LOCAL-002 | P0 / OPEN | `REMOTE_DONE`; Curtain host/frame/panel atomicity, ownership, Health, Family writes and P01-P12 probes exist. H.1 source fix is integrated; stale P06 text is historical only. | Licensed broad H.1/final runtime matrix only. Do not re-code Curtain or #3621 locally. |
| LOCAL-003 | P0 / IN_PROGRESS | `SOURCE_INTEGRATED / AUTOMATED_RUNTIME_PROBE_PASS`; shared Level Z-chain is implemented. | Full licensed dual-unit/Undo/save-reopen/multi-DWG/private matrix only. |
| LOCAL-004 | P0 / IN_PROGRESS | Source Reconcile implementation plus P01-P05 native runners/probes are integrated and bounded cells passed. | Broader #80 topology/category/dependent/failure matrix only. |
| LOCAL-005 | P1 / SOURCE_FIX_READY | #3715 licensed Undo divergence returned to remote source; PR #3727 merged as `ba6e1c7508086beb8ac5db9a4a78d2c43fc09492` and current `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0` contains it. Builder now stages one document-bound native revision before generated topology mutation and publishes coherent semantic history only after CAD commit while preserving rollback/ownership/12,000-bar guards. | On one exact descendant, run multi-region build -> native Undo -> native Redo first. Resume refresh/add/remove/corrupt/cap/Foundation/save-reopen/multi-DWG only after that cell is coherent. |
| LOCAL-006 | P1 / SOURCE_FIX_READY | #3721 licensed Semantic Tag Undo divergence returned to remote source; PR #3728 merged as `887173f28126b928765e458f28202e83a6f3b88f` and current `4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0` contains it. Semantic Tag now stages the established native revision before MText/MLeader mutation and publishes semantic history only after CAD commit. | On one exact descendant, run `QS3DTAG -> native Undo -> native Redo` first. Resume MLeader/Table/Sheet/Layout/Viewport/Unicode/HiDPI/save-reopen/multi-DWG only after that cell is coherent. |
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

The worktree must be clean before qualification. Once #3754 is merged and an exact corrected carrier is published, #3681 needs no separate build/manual fixture procedure; run exactly:

```powershell
.\scripts\run-local-v25-wall-contact-3681.ps1
```

The runner auto-detects a standard BricsCAD V25 installation; `-BricsCadDir` is only an environment override when V25 is installed elsewhere. Other V25 rows may use the general `scripts/run-local-v25-qualification.ps1` plus the row-specific committed runner/runbook. V26 rows follow `docs/LOCAL-V26-QUALIFICATION.md` and must use the matching V26 `net8.0-windows` plugin, not a relabeled V25 binary.

## Fail/return routing

A local result is one of:

- `PASS`: sanitized evidence tied to exact source SHA/plugin identity;
- `FAIL`: reproducible product/runtime failure with sanitized stage/code and enough bounded evidence for a new/existing remote source lane;
- `NO_RESULT`: environment/license/host-start/operator-session/fixture blocker; no product failure inferred.

A `FAIL` never authorizes a local worker to absorb normal source-safe production work. The source correction is implemented and pushed remotely, then the same local matrix is rerun against the new exact SHA.

## Repository/privacy boundary

Never commit BricsCAD proprietary DLLs, license files, activation material, signing keys, customer/private DWGs, raw handles/ProjectIds/fingerprints, crash dumps, private screenshots, browser history, or unsanitized runtime logs. Keep such runtime material under ignored local artifacts and publish only the sanitized evidence required by the row/runbook.
