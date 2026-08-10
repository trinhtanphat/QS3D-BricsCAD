# QS3D — remaining local / external issue index

Updated: 2026-08-10 (UTC+7)

This file is a compact handoff for agents that actually have interactive Windows + licensed BricsCAD V25, approved engineering inputs, or release credentials. It does **not** replace the detailed runbooks:

- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-V25-WPF-SMOKE.md`
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`
- `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`
- `docs/REMOTE-AGENT-SCOPE.md`

Always fetch current `main` before acting because remote agents are continuously advancing source-safe parts of these issues.

## Runtime / workstation gates

- **#72 — exact-SHA BricsCAD V25 qualification.** Run the canonical local runner, then the interactive/private-DWG matrix on the same SHA/package. Required evidence includes V25 adapter build, NETLOAD/DemandLoad, Direct Draw/UCS, Door/Opening, Room/HT_PHÒNG, Curtain, current rebar families, save/reopen/multi-DWG, BQ/BBS/Excel, Unicode/HiDPI and clean install/upgrade/uninstall. `-SkipRuntime` cannot create a release-qualified result.
- **#82 — real V25 UI/DPI/Ribbon/context-menu polish.** Validate real host rendering at 100/125/150/200% DPI, narrow/normal/wide palettes, Vietnamese/long text, popups, focus/disabled/read-only states, docking/floating and splitter persistence. `scripts/run-local-v25-wpf-smoke.ps1` is only an early offline failure detector.
- **#81 — large-model performance.** Measure representative projects in V25 before optimizing native/editor/database paths. Keep raw private project evidence out of Git.

## Native authoring / geometry gates

- **#74 — Direct Draw transient preview / repeated authoring.** Real DrawJig/transient/editor lifecycle, OSNAP/ORTHO, ESC/UNDO and repeated placement require local proof. Preview must remain ownership-neutral and cancellation must leave no residue.
- **#73 — multi-owner wall solids / advanced wall and Curtain geometry.** Do not blind-union semantic owner solids. Any physical L/T/X/Multi reconciliation or broader freeform Curtain behavior needs an ownership-safe replacement/recovery contract plus exact-V25 proof.
- **#83 — generalized polygonal Slab/Foundation mesh.** `PolygonScanlineClipper` + `PolygonalSlabMeshPlanner` + `PolygonalSlabMeshSmoke` are now `REMOTE_DONE`, including bounded simple concave polygons, independent X/Y spacing/count, top/bottom faces, rectangle compatibility and Euclidean boundary clearance using `cover + bar radius`. **Do not reimplement or re-audit that Core slice remotely.** Local agents now own wiring the existing `QS3DSLABREBAR3D` / `QS3DFOUNDATIONREBAR3D` native builders to the planner using a bounded closed-POLYLINE/bulge tessellation path, preserving the current one-transaction rollback/ownership/stale/health contracts; define explicit opening/hole-loop semantics rather than approximating them; then prove real generated bars, save/reopen and rollback on the exact V25 SHA.
- **#79 — Grid/reference + Level native integration.** Source already has `QS3DGRID` and Core Bottom/Top Level-reference semantics. Remaining native placement/UI must make host solids, openings, Curtain and dependent rebar consume one coherent vertical resolver before it is exposed as complete.
- **#80 — richer semantic modify/edit workflow.** `QS3DSYNCSOURCE` materially advances authoritative-source reconcile. Interactive MOVE/ROTATE/STRETCH/grips, UNDO/document-switch/save-reopen behavior still needs real V25 qualification and must preserve provenance/rollback/dependency invalidation.
- **#77 — native documentation layer.** Core `SemanticTagRenderer` exists, but MText/MLeader/Table/Layout/Viewport ownership/replacement, Model/Paper Space behavior, Unicode and save/reopen remain native/runtime work. Read `docs/DOCUMENTATION-LAYER.md`.

## Engineering / policy / release gates

- **#76 — fabrication-grade rebar / structural depth.** Do not invent hooks, laps, anchorage, bend radii or code-specific detailing. A governing standard + revision and explicit engineering inputs/approval are required before implementation can claim fabrication/code behavior.
- **#75 — production signing/install/update + optional licensing.** Real Authenticode certificate custody, trusted timestamp, publisher/thumbprint trust, clean-machine lifecycle and signed-package proof require the approved local release environment. Optional licensing/team sync needs explicit owner SKU/seat/trial/binding/offline/key-rotation/backend policy first.
- **#84 — broader interoperability.** Read-only `QS3DINTERCHANGEJSON` exists. Import/round-trip or additional formats require explicit schema/identity collision/unit/provenance/rollback requirements; do not claim unsupported IFC/Revit/BCF/cloud interoperability.
- **Legal/public/source distribution model** remains owner/legal-policy input. Do not choose a license/distribution posture merely to close a technical checklist.

## Evidence rule

Only a local/qualified agent may write `LOCAL_PASS`, and only for the exact SHA/package it actually tested. Remote/static work may be `REMOTE_DONE`; missing local evidence remains `NOT QUALIFIED`, not an invitation for remote agents to manufacture a pass.

Keep proprietary BricsCAD DLLs, private/customer DWGs, signing secrets and unsanitized machine/runtime evidence out of Git. Store raw evidence under ignored `artifacts/` and commit only reusable source/scripts/docs plus a sanitized PASS/FAIL summary when useful.

GitHub Actions remain manual-only under `CI_POLICY.md`; this handoff does not authorize workflow dispatch or release publication.
