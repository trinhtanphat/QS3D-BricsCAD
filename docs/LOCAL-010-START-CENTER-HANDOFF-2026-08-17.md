# LOCAL-010 Start Center handoff — 2026-08-17

This document is **supporting detail only** for the existing canonical local queue item `LOCAL-010 — large-model performance and UI matrix` in `docs/LOCAL-AGENT-INBOX.md`. It does **not** create a second live LOCAL_ONLY queue and it does not claim licensed BricsCAD runtime evidence.

## Source / issue context

- Canonical issue: #1807 — `BLT3D-familiar KHỞI ĐẦU ribbon + Start Center shell`.
- Source implementation from the original shell lane is integrated; later Start Center source hardening has also landed on `main`.
- This handoff records the remaining interactive Windows/BricsCAD visual, routing, lifecycle, DPI, and document-affinity acceptance that cannot be established by remote/static CI.
- Remote disposition after publication: `LOCAL_ONLY / PARKED / DO_NOT_RETRY_REMOTE`.

## LOCAL-010 Start Center scenario

On a clean exact candidate, using the matching QS3D assembly for the installed BricsCAD host major:

1. Activate the QS3D `KHỞI ĐẦU` tab and verify the `Dự án` and `Cấu hình` Ribbon groups are visible, distinct, correctly labeled, and render their intended icons/text. Repeated Ribbon initialization/reload must not duplicate QS3D-owned panels or controls.
2. Verify `KHỞI ĐẦU` opens the QS3D Start Center inside the BricsCAD-hosted plugin lifecycle. It must not create a standalone QS3D application/process as the user-facing Start Center surface.
3. Verify the BLT3D-familiar two-column Start Center shell, Recent Projects pane, quick workflow actions, and bottom status strip render without clipping, overlap, inaccessible hit targets, or stale state. The status strip includes the intended `Mô hình`, `BQ`, floor/elevation, display/contrast/ortho/snap affordances present in the exact candidate.
4. Exercise the primary routes available in the exact candidate, including Create/Open, Save, Save As, Settings/System Objects, Recent Project where populated with authorized test data, and the `Mô hình` / `BQ` routes. Each user action must dispatch the intended BricsCAD/QS3D behavior exactly once; no duplicate dispatch, dead hit target, or unintended command may occur.
5. Repeat the interaction matrix across document activation/switching, Start Center close/reopen, Ribbon rebuild/reload, and project/cache reload boundaries that are supported by the exact candidate. A stale Start Center bound to another document must not mutate or dispatch into the active DWG.
6. Repeat representative rendering and interaction checks at 100%, 125%, 150%, and 200% DPI with narrow, normal, and wide BricsCAD window/palette layouts. Verify text, icons, cards, quick actions, recent-project controls, and the bottom strip remain legible and actionable without clipping.
7. Keep host-major identity strict. V25 and V26 evidence must identify the matching assembly/ProductVersion and must not relabel or reuse one host-major binary as evidence for another.

## Evidence required

Record sanitized evidence tied to the exact tested SHA:

- exact Git SHA plus QS3D plugin/Core `ProductVersion`;
- Windows version, BricsCAD host major/build, and relevant display/DPI configuration;
- sanitized Ribbon and Start Center screenshots at representative DPI/window sizes, excluding private paths/customer data;
- visible QS3D Ribbon panel/control counts before and after rebuild/reload;
- result for each exercised Create/Open/Save/Save As/Settings/System Objects/Recent Project/`Mô hình`/`BQ` route, including proof that one user action produces one intended dispatch;
- Start Center host-lifecycle result proving no separate user-facing standalone QS3D process is used for the hosted shell;
- document-switch/reopen/reload results, including explicit no-stale-action and no-cross-DWG mutation/dispatch result;
- explicit no-duplicate-panel/control result after repeated Ribbon initialization;
- any failure reported with the exact failing scenario and exact tested SHA rather than generalized to unrelated source lanes.

## Boundary

Only a compatible local agent with real Windows + licensed BricsCAD evidence may record `LOCAL_PASS`. Remote/source-only agents must not repeatedly execute, poll, or re-audit this matrix merely to fill a status report. The canonical live queue remains `docs/LOCAL-AGENT-INBOX.md` / `LOCAL-010`; this file is the detailed execution/evidence handoff for issue #1807.
