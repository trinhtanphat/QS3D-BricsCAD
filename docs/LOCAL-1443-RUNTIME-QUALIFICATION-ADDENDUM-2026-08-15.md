# #1443 licensed BricsCAD V25 qualification addendum

**Date:** 2026-08-15 (UTC+7)  
**Disposition:** `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`  
**Canonical queues:** `docs/LOCAL-AGENT-INBOX.md` entries `LOCAL-001` and `LOCAL-003`; issue #72 for LOCAL-003 evidence  
**Source recovery:** PR #1544, exact head `9f4d28d6951a03e3084759d913e85c07c6f87564`  
**Curved/round runtime harness recovery:** stacked PR #1548, exact head `0b44b69677f3c5cb8b6787bd30fee6027ecb2c7a`  
**Original reviewed implementation:** PR #1472 / `067ec63931a7a4c48849ddb87958155964bb6b3e`  
**Original reviewed harness:** PR #1528 / `687ea56d66e5a3359c3107e66c4f97a319a49587`

This file is a qualification addendum, not a second live LOCAL queue. It exists because the original #1472 and #1528 work targeted the now-stale `integration/20260815-merge-all-v2`. The reviewed production blobs are recovered on current-main ancestry by #1544, and the reviewed additive runtime-harness blobs are recovered by stacked PR #1548. The canonical LOCAL status remains outside this document.

No source/static result in #1544 or #1548 is licensed runtime evidence. Do not publish `LOCAL_PASS`, `P10_PASS`, or production-complete status until a licensed BricsCAD V25 x64 worker executes the final intended exact SHA and posts sanitized evidence to the canonical queue.

## LOCAL-003 — curved and round structural capture-to-build

Run only after the authorized coordinator has landed/reconciled the intended production source and runtime harness and locked one exact candidate SHA. The plugin/Core build, `ProductVersion`, repository HEAD, and recorded SHA-256 must correspond to that exact SHA.

Exercise the real production route:

`QS3DBEAM / QS3DSLAB / QS3DCOLUMN -> SemanticCaptureService -> EntitySnapshotCaptureEligibility -> EntitySnapshotReader -> QS3DBUILD3D -> StructuralSolidBuilder`

### Required source matrix

Beam:

- LINE control;
- planar WCS-XY ARC;
- planar WCS-XY CIRCLE;
- open straight POLYLINE;
- open curved/bulged POLYLINE.

Round profiles:

- planar WCS-XY CIRCLE Slab;
- planar WCS-XY CIRCLE Column.

Fail-closed controls:

- closed Beam POLYLINE;
- non-WCS Beam CIRCLE;
- invalid/non-finite/over-budget geometry where safely reproducible.

### Required acceptance

For accepted cases, prove finite positive captured Length/Area, expected native bounds and Z placement, generated `Solid3d` ownership, and one coherent generated result. For curved Beam paths, inspect for missing/disconnected segments, unintended overlap, failed union, or ownership loss. For CIRCLE Slab/Column, verify radius-derived area and expected extrusion profile/vertical placement.

Run both Millimeter and Meter drawing fixtures. Verify rebuild replacement/retirement continuity and no foreign-object deletion. Representative curved Beam and round Slab/Column cases must also satisfy the applicable broader LOCAL-003 lifecycle matrix: Undo/Redo, save/reopen, stale rebuild behavior, and multi-DWG isolation on the same intended candidate before production-complete status.

### Dedicated recovered harness

PR #1548 recovers the reviewed additive harness from #1528 without changing production behavior:

- `src/QS3D.BricsCAD.V25/CurvedStructuralRuntimeProbeCommands.cs`;
- `scripts/test-bricscad-v25-curved-structural.ps1`;
- `scripts/preflight-curved-structural-runtime.py`.

The runner requires a clean exact repository HEAD, matching `ProductVersion +SHA`, plugin SHA-256 evidence, licensed BricsCAD V25 x64, and a guarded disposable read-only DWG copy. Marker output is intentionally sanitized and cannot manufacture `LOCAL_PASS` or `P10_PASS`.

The existing `scripts/test-bricscad-v25-level-z.ps1` / `QS3DLEVELZPROBE` remains useful placement prerequisite evidence, but its representative Beam is LINE-only and it does not qualify ARC/CIRCLE/curved open-POLYLINE Beam or CIRCLE Slab/Column behavior.

## LOCAL-001 — automatic update discovery remains non-modal

The #1443 production recovery removes automatic presentation of Update Center from `UpdateBootstrapper.OnAutomaticUpdateFound(...)` while preserving the explicit `QS3DUPDATE` route.

On the same locked exact candidate, exercise:

- cold start with no update available;
- safe update-available condition when repository-approved test infrastructure supports it;
- safe offline/update-check failure behavior;
- active drawing plus a second-DWG activation while discovery completes;
- explicit `QS3DUPDATE` after startup.

Acceptance:

- automatic discovery must not open Update Center or any other modal updater window;
- startup/editor command input must not be blocked or require dismissing a dialog;
- an available update may surface only through the existing non-modal notification path;
- no-update/offline/failure cases remain non-modal and non-destructive;
- explicit `QS3DUPDATE` still opens Update Center on user request;
- no duplicate updater windows or cross-DWG stale callbacks;
- discovery does not mutate semantic project state, CAD entities, selection, audit state, or drawing bytes.

`UpdateManifestProbe` remains package/trust evidence only. Source inspection of `OnAutomaticUpdateFound(...)` is source evidence only. Neither substitutes for licensed host behavior.

## Five Sheet fixes re-audited remotely

No duplicate guard is justified unless current source materially changes the guarded contract or a concrete regression is demonstrated:

1. Direct Draw current-view preservation: `scripts/preflight-direct-draw-view-preservation.py`.
2. Family quick workflow: `scripts/preflight-family-manager-qs-quick-workflow.py`.
3. `QS3DSETUP`: existing modal-host, unsaved-close, and rule-management guards.
4. `slabOpen`: existing negative-Z Boolean and first-use host auto-build guards.
5. Quantity explanation/detail: `scripts/preflight-quantity-insight-detail.py`.

## Recovery and integration boundary

- PR #1544 recovers exactly the four reviewed #1472 production/guard files onto current-main ancestry.
- PR #1548 is intentionally stacked on #1544 and recovers exactly the three reviewed #1528 runtime-harness files.
- The stale-base PR #1528 is superseded by #1548; no harness code was discarded.
- The old docs PR #1499 is superseded by the replacement PR for this file once opened.
- `docs/LOCAL-AGENT-INBOX.md` remains a shared coordination surface and is deliberately not edited by this recovery lane.

This addendum does not authorize a `main` merge, manual GitHub Actions dispatch/rerun, release publication, or production qualification. An authorized coordinator must reconcile/land the recovery PRs, lock the intended exact SHA, and then a licensed local worker must execute the required matrices before the canonical LOCAL status can advance.