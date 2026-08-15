# #1443 / #1472 licensed V25 qualification addendum

**Date:** 2026-08-15 (UTC+7)  
**Disposition:** `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`  
**Canonical queue:** `docs/LOCAL-AGENT-INBOX.md` (`LOCAL-001` and `LOCAL-003`)  
**Source PR:** #1472  
**Task head:** `067ec63931a7a4c48849ddb87958155964bb6b3e`  
**Integration merge:** `58f0262612f19e90fc78160571fa2e3b0e69e9a8`

This document is a qualification addendum, not a second live LOCAL queue. The canonical status remains in `docs/LOCAL-AGENT-INBOX.md`. It records the exact runtime scenarios introduced or materially changed by #1472 so a licensed local worker can qualify them without re-auditing the remote-safe source work.

## LOCAL-003 addendum — curved and round structural capture-to-build

Run from a clean exact candidate SHA that contains #1472. Use licensed BricsCAD V25 x64 and the exact matching QS3D adapter/Core build.

### Beam source matrix

Exercise the real production route `QS3DBEAM -> SemanticCaptureService -> EntitySnapshotCaptureEligibility -> QS3DBUILD3D -> StructuralSolidBuilder` with:

- LINE control;
- planar WCS-XY ARC;
- planar WCS-XY CIRCLE;
- open POLYLINE with straight segments;
- open POLYLINE containing at least one supported bulged/curved segment.

For each accepted source, prove semantic capture records a finite positive curve length, the element remains eligible for structural build, and `QS3DBUILD3D` creates one coherent owned Beam result following the intended path. For ARC/CIRCLE/bulged paths, inspect native geometry closely enough to detect missing segments, disconnected pieces, unexpected self-overlap/failure, incorrect Z placement, or ownership loss. Exercise representative dimensions near normal production values plus a bounded high-segment case. Unsupported/non-planar/non-finite/over-budget input must fail closed without partial semantic/native replacement.

### Slab / Column round-profile matrix

Exercise the real production routes `QS3DSLAB` and `QS3DCOLUMN` with planar WCS-XY CIRCLE sources. Prove the snapshot Area equals the native radius-derived `pi*r^2` within drawing-unit tolerance, the finite positive Area passes semantic eligibility, and `QS3DBUILD3D` creates the expected owned Solid3d extrusion with correct XY radius/profile and vertical placement.

Cover legacy/no-Level plus representative Bottom-only and Bottom+Top placement where applicable, and run at least one Millimeter drawing and one Meter drawing. Unsupported circle orientation or invalid geometry must fail closed without replacing an existing valid generated set.

### Cross-cutting evidence

Record only sanitized evidence:

- exact tested repository SHA and matching plugin/Core `ProductVersion` plus adapter SHA-256;
- BricsCAD V25 build and drawing unit for each run;
- source entity kind, finite Length/Area summary, expected/observed native bounds and Z range;
- generated ownership/fingerprint/stale-rebuild result;
- Undo/Redo, save/reopen and a two-DWG isolation check for representative curved Beam and round Slab/Column cases;
- no cross-DWG deletion, no foreign-object deletion and no private/customer drawing data.

Source/static coverage from `scripts/preflight-sheet-residual-structural.py` is not `LOCAL_PASS`. Until the exact runtime matrix above passes, #1472 curved/round structural behavior remains `PENDING_LOCAL` under `LOCAL-003`.

## LOCAL-001 addendum — startup/update discovery must remain non-modal

Run from the same clean exact candidate SHA that contains #1472.

#1472 removes automatic startup presentation of the Update Center while preserving explicit `QS3DUPDATE` behavior and the automatic-update notification event path. Qualify this in the real host rather than inferring it from source tokens.

Exercise:

- cold BricsCAD start with QS3D loaded and no update available;
- cold start with an update-available condition if the repository-approved test/update endpoint can provide one safely;
- offline/update-check failure behavior if safely reproducible;
- an already-open drawing plus a second-DWG activation while automatic discovery completes;
- explicit `QS3DUPDATE` after startup.

Acceptance:

- automatic discovery must not open Update Center or any other modal updater window;
- startup must not steal/block editor command input or require dismissing an update dialog;
- an available update may surface only through the existing non-modal notification/event path;
- no-update and failure/offline cases must remain non-modal and non-destructive;
- explicit `QS3DUPDATE` must still open Update Center on user request;
- no duplicate Update Center windows or cross-DWG stale callbacks;
- update discovery must not create/mutate QS3D semantic project state, CAD entities, selection, audit state or drawing bytes.

Record exact SHA, BricsCAD/plugin identity, sanitized startup/update outcome, explicit-command result, active-DWG behavior and before/after mutation invariants. Do not record credentials, private update URLs or machine-private paths.

Source/static coverage from `scripts/preflight-sheet-residual-structural.py` is not `LOCAL_PASS`. Until this exact host behavior is demonstrated, the startup/update portion remains `PENDING_LOCAL` under `LOCAL-001`.

## Five landed Sheet fixes — remote regression audit

The #1443 residual audit found no justified duplicate guard addition for the five already-landed Sheet fixes:

1. Direct Draw current-view preservation already has `scripts/preflight-direct-draw-view-preservation.py`, with the generic Direct Draw gate aligned not to require `QS3DVIEW3D`.
2. Family Manager QS quick workflow already has `scripts/preflight-family-manager-qs-quick-workflow.py`, including canonical CAD project-context coverage.
3. `QS3DSETUP` already has focused modal-host, unsaved-close and rule-management guards.
4. `slabOpen` already has the negative-Z Boolean contract guard plus first-use host auto-build guard.
5. Quantity explanation/detail already has `scripts/preflight-quantity-insight-detail.py`, including detached/read-only data, metric rendering and Locate wiring.

No duplicate source/static guard is added by this addendum. A future agent should only reopen one of these five lanes if current source materially changes the guarded contract or a concrete new regression is demonstrated.

## Integration boundary

This addendum does not authorize a `main` merge, manual GitHub Actions dispatch, release publication, or promotion to production-qualified status. Integration and exact-SHA CI remain separate owner/coordinator actions under repository policy.
