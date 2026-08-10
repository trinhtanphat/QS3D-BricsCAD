# QS3D — remaining work / local-agent handoff

Updated: 2026-08-10 (UTC+7)

This note is the execution map for work that cannot truthfully be called complete from remote GitHub/source inspection. It complements `docs/LOCAL-V25-QUALIFICATION.md`; it does not replace the canonical product/status docs.

## Rules for every next agent

1. Fetch current `main` again before editing or validating; this repository is actively modified by multiple agents.
2. Preserve the BricsCAD V25 x64 plugin boundary in `docs/PRODUCT-BOUNDARY.md`.
3. Never turn a source/static result into a runtime claim. Exact-current-SHA V25 compile/NETLOAD/native behavior needs licensed local BricsCAD V25.
4. Do not dispatch GitHub Actions because of `continue all`, docs, source changes or merges. `CI_POLICY.md` remains manual-only.
5. Never commit BricsCAD proprietary DLLs, private/customer DWGs, raw private screenshots, certificates/private keys or machine secrets.
6. Use `docs/LOCAL-V25-QUALIFICATION.md` and `scripts/run-local-v25-qualification.ps1` for local qualification. The runner now includes source/Core/adapter checks plus WPF theme and Workspace/RightPanel layout smoke before the licensed NETLOAD probe.

## A. Mandatory exact-SHA local qualification

**Tracking: #72**

Local agent with interactive Windows + licensed BricsCAD V25 must execute the automated runner and then the manual/private-DWG matrix on the same SHA. Required evidence families include adapter compile against installed V25 assemblies, NETLOAD, DemandLoad, Direct Draw/UCS, Door/Opening cuts, Room/HT_PHÒNG, Curtain, all current rebar families, save/reopen/Save As/multi-DWG, BQ/BBS/Excel, Unicode/HiDPI, clean install/upgrade/uninstall and signing only when approved credentials exist.

Do not close #72 from a remote/static review. Keep raw evidence under ignored `artifacts/`; commit only a sanitized text summary if useful.

## B. BLT-style authoring still incomplete

**Tracking: #74, #80**

- Transient thickness/profile preview and fast repeated Direct Draw authoring need a real V25 DrawJig/transient/editor lifecycle proof. Preview must never become persistent ownership and ESC must leave no residue.
- A richer native modify/edit workflow still needs an explicit source-of-truth and rollback contract. Source-derived measurements remain read-only when CAD source is authoritative; do not invent a second semantic geometry model.
- Optional compact in-command Family selection should be added only if local UX evidence shows the current active-Family workflow is insufficient.

Remote agents may prepare deterministic plans/tests and adapters that fail closed; local agents own interactive proof.

## C. Wall / Curtain / advanced native geometry

**Tracking: #73**

Still not solved broadly:

- physical L/T/X/Multi wall-solid reconciliation/union under a safe multi-owner replacement/unmerge/rebuild contract;
- richer multi-segment WallPier profile authoring around corners;
- broader Curtain/freeform/panel-by-panel backing geometry beyond the currently guarded paths;
- arbitrary complex corner-crossing opening booleans beyond current guarded source paths.

Do not solve these by blind boolean union or ownership guessing. Design deterministic ownership/replacement semantics first, add Core planners where CAD-independent, then validate native operations in V25.

## D. Rebar / structural commercial depth

**Tracking: #76, #83**

- Advanced StructuralWall zones, multi-zone Beam reinforcement and richer editing/manipulation remain product work.
- Hooks, bend radii, lap/anchorage and code/fabrication-specific output may be produced only from explicit engineering inputs/rules; QS3D must not infer structural design.
- Slab/Foundation mesh beyond guarded rectangles needs deterministic polygon clipping that cannot place bars outside the host, while preserving cover/faces/direction settings, bounded counts, ownership/stale/health behavior.

Core math/semantics can be implemented remotely. Native final qualification remains local V25 work.

## E. Reference model and documentation layer

**Tracking: #79, #77**

- Current Floor/Level data does not yet equal a complete first-class Level/Grid reference system. Grids and richer level/elevation constraints need explicit IDs, migration, references and UI/runtime behavior.
- `BottomOffsetM` / `TopOffsetM` must not be relabeled as full level references until that contract exists.
- A production documentation layer still needs semantic tags/labels, DWG tables and sheet/view workflows where supported by V25, all bound to stable semantic IDs/units/provenance rather than decorative disconnected annotations.

## F. Production UI and performance

**Tracking: #82, #81**

- Premium source UI is broad, but real-host visual qualification remains: 100/125/150/200% DPI, narrow/normal/wide palettes, Vietnamese and long text, ComboBox popups, keyboard focus, disabled/read-only/selected states, Ribbon grouping/icons/context menus and splitter persistence.
- Performance must be measured on representative large projects for room topology, junction graphs, Auto Host, Curtain grids, BQ/schedules, SPLINE sampling, ownership registries, regeneration and rebar/mesh batches. Optimize measured bottlenecks without weakening fail-closed bounds.

The automated local runner's WPF smoke is an early failure detector, not a replacement for real BricsCAD screenshots/interaction.

## G. Release/signing/external operations

**Tracking: #75**

Production Authenticode certificate custody, timestamping, signed-manifest/version binding proof, clean-machine install/upgrade/uninstall and publisher/thumbprint enforcement remain external/local release work. Never commit signing secrets and never lower BricsCAD `SECURELOAD`.

Optional licensing/team sync requires a separate explicit owner backend/credential requirement. Do not add fake licensing or a hard-coded service merely to mark a roadmap box complete.

GitHub Release publication and workflow dispatch remain separately owner-authorized operations under `docs/MANUAL-BUILD-RELEASE.md`.

## H. Interoperability

**Tracking: #84**

Current supported exchange remains centered on `.qsdb`/template persistence and existing XLSX/CSV/reporting paths. Broader import/export must be specified explicitly with stable IDs, units, provenance and fail-closed validation. Do not add proprietary dependencies or unsupported format claims without an explicit requirement/specification.

## Completion definition

An issue can be closed only when its own source contract/tests are present and every required runtime/external gate for that issue has actually been executed on the fixed exact SHA. For runtime-gated work, a local agent should record a sanitized PASS/FAIL summary and link the fixing commit(s); never change FAIL to PASS without rerunning the affected scenario.

Current source should continue to be described precisely as **source-implemented / statically guarded where applicable, with licensed BricsCAD V25 qualification pending for runtime-gated behavior**.
