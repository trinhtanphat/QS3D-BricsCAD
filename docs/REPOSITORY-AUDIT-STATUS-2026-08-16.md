# QS3D-BricsCAD repository audit status — 2026-08-16

Audit baseline when this table was created: protected `main@8ddea334f0af1136e97c881d32907e0a0f20249f`.

This is a living engineering-status table, not a blanket production-readiness claim. `PASS` means the stated repository/CI contract is evidenced at the cited scope. Native BricsCAD behavior remains `PENDING_LOCAL` until an exact-SHA licensed host run exists. External signing/install requirements remain blocked until the approved environment/credentials are actually used.

| Hạng mục | Đánh giá | Kết luận audit |
| --- | --- | --- |
| Protected `main` | ✅ PASS | Active `protectedMain`: PR required, strict required checks `preflight` + `core`, no force/non-fast-forward path and no configured bypass. Keep this enforcement. |
| Issue → branch → CI → PR governance | ✅ PASS | Normal agents keep `main` read-only; work is registered in an Issue, implemented on `agent/**`, validated on the branch, then proposed by PR. Generic `fix/continue all/commit push` language does not self-authorize merge. |
| Branch CI before new PR | ✅ PASS | Watched task branches run Shared Branch CI before a new PR. A new/updated candidate must use evidence from its exact head SHA. |
| PR merge-candidate CI | ✅ PASS | Every PR to `main` emits stable `preflight` and `core` contexts; path classification may skip redundant builds but cannot make protected required contexts disappear. |
| Governance preflight executable modes | ✅ PASS | #1813 was repaired through #1815 and landed on `main@8ddea334f0af1136e97c881d32907e0a0f20249f`; governance preflight scripts are executable again. |
| V25 cloud preview | ✅ PASS (cloud scope) | Exact-main preview dispatch/build/package/release is operational. Release preparation is workspace-only and does not push/commit to protected `main`. Cloud PASS is not licensed NETLOAD/runtime PASS. |
| V25 commercial release architecture | ✅ PASS (source/workflow scope) | Build/sign/runtime candidate is separated from the write-enabled publication job, with checksum/provenance/signature and post-upload byte verification before publication. Production certificate + clean-machine acceptance remain separate evidence. |
| V26 release least privilege | 🛠 ACTIVE #1817 | Being hardened to `build_sign` on self-hosted V26 with `contents: read`, artifact boundary, then a GitHub-hosted publication job with `contents: write` plus candidate re-verification. Do not mark PASS until branch/PR CI validates the new workflow contract. |
| Immutable GitHub Actions references | 🛠 ACTIVE #1817 | All external workflow actions are being pinned to exact 40-hex commit SHAs; `preflight-github-actions-pins.py` fails closed on mutable refs. Dependabot major upgrades still require their own compatibility CI and no auto-merge authority. |
| Checkout credentials / token minimization | 🛠 ACTIVE #1817 | Checkout credentials are disabled where repository credentials are unnecessary; write permission must remain isolated to explicit publication/dispatch jobs. |
| Measurement trace reconciliation | 🛠 FIXED ON BRANCH #1803 | Clean current-main replacement branch `agent/chatgpt-gpt56sol/measurement-trace-overflow-r2` contains the bounded overflow-safe reconciliation fix + regression. Await exact branch CI/PR; current `main` must not yet be called fixed. |
| IFC round-trip UTF-16 canonical tokens | 🛠 FIXED ON BRANCH #1814 | `agent/chatgpt-gpt56sol/ifc-projection-utf16-1814` rejects unpaired UTF-16 surrogates and retains valid surrogate pairs. Await exact branch CI/PR. |
| KHỞI ĐẦU/Ribbon command dispatch | 🛠 FIXED ON BRANCH #1810 | `agent/chatgpt-gpt56sol/fix-ribbon-command-dispatch-1810-r4` captures the intended command and supplies it when BricsCAD omits `ICommand` parameters; deterministic source guard added. Licensed click behavior remains `PENDING_LOCAL`. |
| V25 licensed exact-SHA qualification | ⚠️ PENDING_LOCAL #72 | Cloud/static/build evidence cannot replace interactive licensed V25 NETLOAD, save/reopen, Undo/Redo, multi-DWG, UI/DPI/context-menu and representative scenario evidence. |
| V26 build/runtime/release qualification | ❌ NOT YET QUALIFIED #1462 | Source/workflows exist, but the dedicated V26 qualification/release lanes still require the first exact-SHA self-hosted licensed run and release evidence. Do not substitute V25 evidence. |
| Production signing + clean install/update/rollback/uninstall | ⚠️ BLOCKED_EXTERNAL #75 | Source tooling is fail-closed, but production readiness still requires the approved Authenticode certificate/timestamp, clean-machine install/upgrade/rollback/uninstall and BricsCAD SECURELOAD/DemandLoad proof. |
| Repository-wide source guards + Core/V25 compile | ✅ PASS FOR LAST GREEN EXACT-MAIN EVIDENCE | Aggregate feature preflights auto-discover `scripts/preflight-*.py`; Core deterministic smoke and V25 plugin compile are part of shared/release validation. New active defect branches above must still pass their own exact-head CI before integration. |

## Completion rule

Do not label the repository `100% COMPLETE` while any row remains `ACTIVE`, `PENDING_LOCAL`, `NOT YET QUALIFIED`, or `BLOCKED_EXTERNAL`. A row moves to `PASS` only after its exact implementation/evidence lane is integrated and the corresponding source, CI, native-runtime, or external acceptance boundary has actually been exercised.
