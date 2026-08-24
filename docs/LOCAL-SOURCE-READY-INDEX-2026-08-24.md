# LOCAL_ONLY source-ready index — 2026-08-24

Status: `SOURCE_READY / LOCAL_RUN_ONLY`

Parent: #72

Lane-Key: `issue-3680`

Canonical coordination branch: `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh`

Latest observed merged main at this sweep: `16610f51628b1db7491c7706eee92639ec736330`

This index records the owner-requested boundary: remote/source work is prepared and pushed first; a compatible local agent should only fetch the named exact carrier, build/run the licensed BricsCAD matrix, and publish sanitized `PASS` / `FAIL` / `NO_RESULT`. A local runtime defect that requires production-source correction returns to a bounded remote/source lane. Local workers must not opportunistically patch normal production source.

This file does **not** manufacture `LOCAL_PASS`. Runtime status remains governed by `docs/LOCAL-AGENT-INBOX.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-V26-QUALIFICATION.md`, and the linked per-feature runbooks.

## Immediate exact-SHA dispatch

These carriers are source-ready and form the first executable queue from `docs/LOCAL-DISPATCH-READY-2026-08-24.md`:

| Priority | Issue | Exact carrier | Exact runtime-source SHA | Local action |
| --- | --- | --- | --- | --- |
| P0 | #1744 | `agent/control01/slabopen-undo-semantic-1744` | `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31` | fetch, checkout, build, run only |
| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh` | exact validated runner SHA published on #3681; must contain `cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb` | fetch exact SHA, run `scripts/run-local-v25-wall-contact-3681.ps1` only |
| P1 | #3613 | `agent/qs3d-uix-worker-b/issue-3613-coordination-locate-zoom` | `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31` | fetch, checkout, build, run only |

For #3681, `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31` is historical failing runtime evidence only. Source defect #3687 was corrected by PR #3692 at source-fix SHA `cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb`. The final runnable carrier is this coordination branch after the committed #3681 harness/runner lands and protected branch CI is green; the exact immutable carrier SHA is published on #3681 and #72. Local agents do not check out the old source-fix branch directly and do not write test code.

The #3681 local action is deliberately one command after the exact checkout:

```powershell
.\scripts\run-local-v25-wall-contact-3681.ps1
```

That runner performs the repository-safe preflights/build, builds its committed local-only x64/net48 BricsCAD harness, executes the licensed V25 full/partial/union/top-bottom/stale/capture-refresh/two-end cases, runs a second fresh process for isolation, persists a scratch DWG/QSDB and cold-reopens it, checks the BLT `2.6688 - 0.3200 = 2.3488 m²` control, records plugin/Core identity and removes test-owned scratch state. `Undo/Redo` is reported as not applicable to the contact measurement itself only after the harness proves that measurement is read-only; no manual geometry authoring or source patch is delegated to the local agent.

Do not rerun the obsolete #3593 P06 binary. The later #3593 P07 result and closed #3621 source lane are authoritative for that H.1 correction.

LOCAL-019 is already complete, not pending dispatch: PR #3693 merged the six-sheet QS Review export + Excel-to-Model Locate source, and licensed BricsCAD V25/V26 qualification passed on exact runtime-tested source SHA `9cfff87262d7a7117c5ef1f03b486271a0723fa3`. Do not schedule a repeat solely because this source-ready index was refreshed.

## All canonical LOCAL_ONLY rows

| Local row | Priority / status | Remote/source disposition | What remains for the local agent |
| --- | --- | --- | --- |
| LOCAL-001 | P0 / PASS | Source, baseline runner and lifecycle probes are integrated. | Only explicitly unqualified wider interactive/private/release cells; start from `scripts/run-local-v25-qualification.ps1`. |
| LOCAL-002 | P0 / OPEN | `REMOTE_DONE`; Curtain host/frame/panel atomicity, ownership, Health, Family writes and P01-P12 probes exist. H.1 source fix is integrated; stale P06 text is historical only. | Licensed broad H.1/final runtime matrix only. Do not re-code Curtain or #3621 locally. |
| LOCAL-003 | P0 / IN_PROGRESS | `SOURCE_INTEGRATED / AUTOMATED_RUNTIME_PROBE_PASS`; shared Level Z-chain is implemented. | Full licensed dual-unit/Undo/save-reopen/multi-DWG/private matrix only. |
| LOCAL-004 | P0 / IN_PROGRESS | Source Reconcile implementation plus P01-P05 native runners/probes are integrated and bounded cells passed. | Broader #80 topology/category/dependent/failure matrix only. |
| LOCAL-005 | P1 / OPEN | `SOURCE_COMPLETE` through #3647 / PR #3652: native Slab/Foundation multi-region, holes, bulges, OCS/WCS association, per-region ownership and Health are implemented. | Licensed exact geometry, Undo/Redo, save/reopen and multi-DWG qualification only. |
| LOCAL-006 | P1 / OPEN | `SOURCE_COMPLETE / REMOTE_DONE` after PR #3643; MText/MLeader, Tables/custom schedules and Sheet/Layout/PaperSpace/Viewport/title-block paths exist. | Follow `docs/LOCAL-006-NATIVE-DOCUMENTATION-QUALIFICATION.md`; runtime/visual/V26 parity only. |
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

The worktree must be clean before qualification. For #3681 no separate build/manual fixture procedure is required; run exactly:

```powershell
.\scripts\run-local-v25-wall-contact-3681.ps1
```

The runner auto-detects a standard BricsCAD V25 installation; `-BricsCadDir` is only an environment override when V25 is installed elsewhere. Other V25 rows may use the general `scripts/run-local-v25-qualification.ps1`. V26 rows follow `docs/LOCAL-V26-QUALIFICATION.md` and must use the matching V26 `net8.0-windows` plugin, not a relabeled V25 binary.

## Fail/return routing

A local result is one of:

- `PASS`: sanitized evidence tied to exact source SHA/plugin identity;
- `FAIL`: reproducible product/runtime failure with sanitized stage/code and enough bounded evidence for a new/existing remote source lane;
- `NO_RESULT`: environment/license/host-start/operator-session/fixture blocker; no product failure inferred.

A `FAIL` never authorizes local002/local003 or #3681 to absorb normal source-safe production work. The source correction is implemented and pushed remotely, then the same local matrix is rerun against the new exact SHA.

## Repository/privacy boundary

Never commit BricsCAD proprietary DLLs, license files, activation material, signing keys, customer/private DWGs, raw handles/ProjectIds/fingerprints, crash dumps, private screenshots, browser history, or unsanitized runtime logs. Keep such runtime material under ignored local artifacts and publish only the sanitized evidence required by the row/runbook.
