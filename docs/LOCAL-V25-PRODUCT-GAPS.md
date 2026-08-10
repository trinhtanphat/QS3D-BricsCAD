# QS3D — V25 local-only product gaps

Updated 2026-08-10 (UTC+7).

This file complements `docs/LOCAL-V25-QUALIFICATION.md`. The qualification document is the canonical runtime checklist; this file is only for **remaining product/geometry work that a source-only agent must not guess**.

Always fetch the latest `main`, work on an exact clean SHA, preserve the BricsCAD-plugin product boundary, and keep GitHub Actions manual-only. Never commit BricsCAD DLLs, private/customer DWGs, proprietary BLT assets, signing secrets or confidential screenshots.

## 1. Physical multi-owner wall-solid reconciliation

Current safe behavior rebuilds owned wall geometry from source centerlines. A true L/T/X/Multi physical union is not complete product parity because a single unioned solid can represent more than one semantic wall.

Before implementation, the local agent must define and prove:

- shared-solid ownership: how all contributing semantic wall IDs are persisted;
- selection/Locate/BQ behavior when one generated solid belongs to several semantic walls;
- invalidation when only one contributing source wall changes;
- safe unmerge/rebuild semantics;
- opening-cut and Curtain/rebar dependent behavior;
- rollback when Boolean union succeeds partially then a later step fails;
- fail-closed handling of foreign/ambiguous generated handles.

Acceptance requires deterministic rebuild from source geometry, explicit shared-owner Health diagnostics and representative BricsCAD V25 Boolean testing. Do not solve this by assigning the shared solid arbitrarily to the first wall.

## 2. WallPier richer open-POLYLINE profile authoring

Current guarded WallPier Direct Draw/native specialization is deliberately narrow. A richer open-POLYLINE specialized profile path still needs real V25 geometry proof.

The local implementation must keep:

- source geometry as the source of truth;
- deterministic profile planning before native mutation;
- explicit thickness/profile/chamfer semantics rather than inferred shapes;
- ownership-scoped replacement;
- rollback that never erases foreign/ambiguous CAD;
- save/reopen and rotated planar-UCS regression.

Do not collapse WallPier back into a generic wall builder if that loses its specialized profile semantics.

## 3. Curtain panel-by-panel backing glass solids

Current Curtain source supports a backing GlassWall host plus generated frame overlays, including guarded path-frame work. Panel-by-panel backing glass solids are still a separate product gap.

A local implementation needs a deterministic panel identity scheme, generated-handle ownership per panel, opening-aware clipping, stale/rebuild lifecycle and a bounded replacement transaction. It must remain compatible with Curtain schedule/Health and not double-count the existing backing host.

Test LINE, open POLYLINE and bulged path cases on real V25. Do not claim arbitrary freeform/3D path support without a separately proven planner.

## 4. DrawJig thickness/profile preview and repeated authoring

A BLT-familiar transient preview should use BricsCAD's native viewport/editor Jig APIs, not a fake WPF CAD canvas.

Local requirements:

- no persistent source entity before commit;
- ESC/cancel leaves no CAD or semantic residue;
- preview respects World/translated/rotated planar UCS and drawing units;
- tilted/3D UCS remains fail-closed while downstream builders are WCS-planar;
- commit converges through the existing Direct Draw -> semantic capture -> regeneration -> native build ownership path;
- repeated mode has an explicit Finish/ESC state and does not trap BricsCAD command input.

Only add a compact in-command Family picker if real UX testing shows the existing canonical Family / Type workflow is too slow. Do not create a second Direct-Draw-only family store.

## 5. Optional one-shot Door/Opening + physical cut

Current Direct Draw Door/Opening intentionally completes source + semantic + Auto Host without silently performing destructive physical Boolean cutting.

A one-shot placement-and-cut workflow may be considered only after local V25 proves `QS3DCUTSELECTEDOPENINGS` transaction/rollback behavior for:

- one and multiple openings;
- multiple hosts;
- no-host and ambiguous-host placement;
- same-fingerprint idempotent rerun;
- changed cut state fail-closed/rebuild path;
- forced Boolean failure with no half-committed semantic/native state.

Do not auto-enable destructive cutting merely for UX similarity.

## 6. UI/Ribbon/context-menu/keyboard/HiDPI proof

Current source contains the BricsCAD-hosted Ribbon, Workspace/right palettes, dark theme, context menus and keyboard shortcuts. Local V25 must verify 100%, 125%, 150% and 200% scaling.

Pay special attention to:

- Vietnamese clipping/wrapping;
- context-menu foreground/background under BricsCAD host themes;
- right-click selecting the intended Family/CAD row before action;
- `Ctrl+S`, `Ctrl+F`, `Ctrl+B`, `F5` and Family `Delete` acting only while QS3D workspace focus is appropriate;
- no interception of normal BricsCAD command-line input;
- live Layer/Xref color/lock state rather than sample/fake data;
- native BricsCAD viewport remaining the center CAD renderer.

If a shortcut conflicts with BricsCAD in real use, scope/remove the shortcut rather than globally hijacking host input.

## 7. Private-DWG save/reopen/multi-document proof

Use only private local copies of owner-provided representative DWGs; never commit them.

Required cases:

- create/edit/save `.qsdb`, close and reopen;
- Save As / renamed DWG fingerprint behavior;
- two DWGs open concurrently with repeated MDI switching;
- project editors/palettes never mutate the wrong drawing after focus switch;
- semantic Locate/selection remains document-bound;
- dangling references are reported by Health rather than silently rewritten;
- `QS3DRELEASECHECK` on representative populated data.

Commit only sanitized text evidence if the drawing is confidential.

## 8. Large-model performance proof

Record approximate timings and obvious UI stalls for palette refresh, Layer search, Family filtering, BQ build/filter, Model/Full Health, localized regeneration and Room Auto on a bounded representative selection.

Do not move BricsCAD database operations to arbitrary background threads as a shortcut. Native DB/editor work must remain on an appropriate document/thread context.

## 9. Production signing/install/update qualification

This remains external until a real approved signing certificate/key and controlled Windows test machine are available.

When the owner explicitly starts this phase, prove fresh install, upgrade, forced mid-install rollback, uninstall, Authenticode signer/version binding, intentionally mismatched/relabelled package rejection, archive guards and unchanged BricsCAD `SECURELOAD` behavior.

Never commit signing private keys or machine secrets.

## Local-agent completion report

For any item above, report the exact tested SHA, BricsCAD V25 edition/build, PASS/FAIL cases, safe evidence path, source commit(s) and exact blocker still remaining. Never mark an item complete from source inspection alone.
