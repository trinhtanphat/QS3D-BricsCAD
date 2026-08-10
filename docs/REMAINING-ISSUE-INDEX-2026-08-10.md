# QS3D — remaining work issue index

Updated: 2026-08-10 (UTC+7)

This compact tracker complements the detailed local handoffs already on `main`:

- `docs/LOCAL-V25-QUALIFICATION.md` — exact-SHA Windows + licensed BricsCAD V25 qualification;
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` — detailed Curtain-panel, wall-junction, engineering-rebar and production-signing gates;
- `docs/CONTINUE-ALL-HANDOFF-2026-08-10-1710.md` — current continue-all delta and runtime/source boundaries.

Always fetch latest `main` before work. GitHub Actions remain manual-only under `CI_POLICY.md`. Never commit BricsCAD proprietary DLLs, private/customer DWGs, signing secrets, or unsanitized runtime/private-DWG evidence.

For early local WPF failure detection after the V25 adapter is built, agents can run `scripts/run-local-v25-wpf-smoke.ps1`. It checks shared theme resources plus Workspace/RightPanel construction, but it **does not** replace licensed BricsCAD V25 NETLOAD/host-theme/HiDPI/private-DWG qualification.

## Local/runtime qualification

- **#72 — Local-only exact V25 qualification.** Adapter compile against installed V25 assemblies, NETLOAD + DemandLoad, Direct Draw/UCS, Door/Opening booleans, Room/HT_PHÒNG, Curtain, rebar, save/reopen/multi-DWG, BQ/BBS/Excel, Unicode/HiDPI, and clean install/upgrade/uninstall. Static review cannot close this issue.
- **#82 — Real V25 UI/DPI/Ribbon/context-menu polish.** Use runtime screenshots and interaction at 100/125/150/200% DPI; do not infer host-theme/popup/focus behavior from XAML alone.
- **#81 — Large-model performance qualification.** Measure representative projects before optimizing and keep native DB/editor work on safe BricsCAD document/thread contexts.

## BLT-style authoring / editing

- **#74 — Direct Draw transient preview + repeated authoring.** Requires real V25 Jig/transient/editor lifecycle proof. Preview stays ownership-neutral and ESC leaves no residue.
- **#80 — Native semantic modify/edit workflow.** Define authoritative source/provenance, dependency invalidation and rollback semantics before interactive edit/grip behavior.

## Geometry / model depth

- **#73 — Multi-owner wall solids and advanced wall/Curtain geometry.** Never Boolean-union semantic wall owner solids blindly. Preserve ownership, host/opening semantics, invalidation and recoverable replacement.
- **#83 — Generalized polygonal Slab/Foundation mesh.** Add deterministic clipping/planning first; bars must not leave host boundaries and must preserve cover/faces/direction/ownership/health semantics.
- **#79 — First-class Grid/reference model and richer level/elevation constraints.** Evolve the existing `FloorDefinition` model safely; do not create a duplicate level system or relabel current offsets as full level references without a migration/reference contract. Note that current `main` may already contain incremental Grid source work; inspect it before changing this issue.

## Engineering / documentation / interoperability

- **#76 — Fabrication-grade rebar/detailing and broader structural authoring.** Numeric detailing/code rules require an explicit approved governing standard/revision and engineering provenance; QS3D must not infer structural design.
- **#77 — Documentation layer.** Semantic tags/labels, DWG tables and sheet/view workflows should remain bound to stable semantic IDs, units and provenance.
- **#84 — Broader interoperability.** Additional exchange formats require explicit requirements/specifications, stable IDs, units, provenance and fail-closed import validation.

## Release / external operations

- **#75 — Production signing/install/update qualification and optional licensing.** Authenticode key custody, timestamping, clean-machine lifecycle and signed-manifest/version-binding proof are external/local release work. Current source may already contain opt-in signing helpers; they still need approved certificate/timestamp/runtime evidence. Optional licensing/team sync needs a separate explicit owner backend/credential requirement.

## Completion rule

Close an issue only when its source contract/tests exist **and** every runtime, private-DWG, engineering or external gate required by that issue was actually executed on the fixed exact SHA. Raw local evidence belongs under ignored `artifacts/`; commit only sanitized PASS/FAIL summaries when useful.

Until those gates pass, use precise status such as **source-implemented / statically guarded; licensed BricsCAD V25 qualification pending** rather than claiming production/runtime completion.
