# QS3D — remaining work issue index

Updated: 2026-08-10 (UTC+7)

This is a compact tracker for unresolved QS3D work after the current source-hardening pass. It complements, and does not replace, the detailed local execution contracts in:

- `docs/LOCAL-V25-QUALIFICATION.md` — exact-SHA Windows + licensed BricsCAD V25 qualification;
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` — detailed Curtain-panel, wall-junction, engineering-rebar and production-signing gates;
- `docs/LOCAL-V25-PRODUCT-GAPS.md` — local-only geometry/UX/product gaps.

Always fetch latest `main` before work. GitHub Actions remain manual-only under `CI_POLICY.md`. Never commit BricsCAD proprietary DLLs, private/customer DWGs, signing keys/certificates containing secrets, or unsanitized machine/private-DWG evidence.

## Local/runtime qualification

- **#72 — Local-only exact V25 qualification.** Build against installed V25 assemblies, NETLOAD + DemandLoad, Direct Draw/UCS, Door/Opening booleans, Room/HT_PHÒNG, Curtain, rebar, save/reopen/multi-DWG, BQ/BBS/Excel, Unicode/HiDPI, and clean install/upgrade/uninstall. Source/static review cannot close this issue.
- **#82 — Real V25 UI/DPI/Ribbon/context-menu polish.** Use runtime screenshots and interaction at 100/125/150/200% DPI; do not guess host-theme/popup/focus behavior from XAML alone.
- **#81 — Large-model performance qualification.** Measure representative large models before optimizing; keep native DB/editor work on safe BricsCAD document/thread contexts.

## BLT-style authoring / editing

- **#74 — Direct Draw transient preview + repeated authoring.** Requires real V25 Jig/transient/editor lifecycle proof. Preview must be ownership-neutral and ESC must leave no residue.
- **#80 — Native semantic modify/edit workflow.** Define authoritative source/provenance and rollback semantics before adding interactive edit/grip behavior. Source-derived measurements remain read-only when CAD source is authoritative.

## Geometry / model depth

- **#73 — Multi-owner wall solids and advanced wall/Curtain geometry.** Never Boolean-union semantic wall owner solids blindly. Preserve ownership, host/opening semantics, invalidation and recoverable replacement.
- **#83 — Generalized polygonal Slab/Foundation mesh.** Add deterministic clipping/planning first; generated bars must not leave host boundaries and must preserve cover/faces/direction/ownership/health semantics.
- **#79 — First-class Grid/reference model and richer level/elevation constraints.** Do not relabel current source-relative offsets as full Level references until the explicit reference contract exists.

## Engineering / documentation / interoperability

- **#76 — Fabrication-grade rebar/detailing and broader structural authoring.** Numeric code/detailing rules require an explicit approved governing standard/revision and engineering provenance; QS3D must not infer structural design.
- **#77 — Documentation layer.** Semantic tags/labels, DWG tables and sheet/view workflows should remain bound to stable semantic IDs, units and provenance rather than decorative disconnected annotations.
- **#84 — Broader interoperability.** Additional exchange formats require explicit requirements/specifications, stable IDs, units, provenance and fail-closed import validation; do not add unsupported format claims.

## Release / external operations

- **#75 — Production signing/install/update qualification and optional licensing.** Authenticode key custody, timestamping, clean-machine lifecycle and signed-manifest/version-binding proof are external/local release work. Optional licensing/team sync needs a separate explicit owner backend/credential requirement.

## Completion rule

Close a tracking issue only when its source contract/tests are present **and** every runtime, private-DWG, engineering or external gate required by that issue was actually executed on the fixed exact SHA. For runtime-gated items, commit only a sanitized PASS/FAIL summary when useful; raw local evidence belongs under ignored `artifacts/`.

Until those gates pass, use precise status such as **source-implemented / statically guarded; licensed BricsCAD V25 qualification pending** rather than claiming production/runtime completion.
