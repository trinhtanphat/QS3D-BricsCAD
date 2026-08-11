# Work claim — LOCAL-003 shared native Level Z-chain

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-11T19:43:12+07:00`
- Baseline main SHA: `c7dd212d36677a1d2e005becf8709768fe98d6a1`
- Priority: `LOCAL-003 / P0` — close the semantic/native vertical-placement split using the available licensed local runtime

## Reserved scope

Implement and qualify the coherent native Level Z-chain using `ElementVerticalPlacementService` as the single semantic source of truth. Preserve exact legacy source-relative geometry when Level references are absent; support Bottom Level only and Bottom + Top Level placement; fail closed for Top-only, missing/ambiguous Levels, non-finite offsets, and invalid vertical ranges.

The reserved chain covers native host placement plus every generated system that derives Z/height from those hosts, so the product cannot expose a partial Level assignment that leaves openings, Curtain output or reinforcement spatially detached.

## Expected surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`, Level reference/stale/health contracts, and focused smoke coverage only where the shared contract needs strengthening.
- BricsCAD wall/GlassWall/WallPier and structural Beam/Column/Slab/StructuralWall/Foundation native builders.
- Door/WallOpening host-relative matching and physical cutter placement.
- Curtain host, LINE/path frame and native panel placement consumption; no topology or ownership redesign.
- Generated longitudinal/tie/stirrup/shape/slab/wall/foundation reinforcement placement consumption; no fabrication-rule invention.
- Direct Draw and Floor/Level assignment UI only after the complete native/dependent chain is coherent and guarded.
- Focused static preflight(s), Core smoke registration, exact-V25 build/runtime probe or sanitized qualification support needed for `LOCAL-003`.
- `docs/LOCAL-AGENT-INBOX.md` and the existing Level/local handoff documents for exact scenario/evidence status.

## Excluded scope

- No Create Similar work reserved by `2026-08-11-chatgpt-web-create-similar.md`.
- No Workspace multi-selection policy work reserved by `2026-08-11-chatgpt-web-gpt56sol-workspace-multi-policy.md`.
- No modeless schedule/revision viewer identity work reserved by `2026-08-11-chatgpt-web-modeless-viewer-project-identity.md`.
- No Core Navigation/Review/Interchange/Rules mutation atomicity audit reserved by `2026-08-11-chatgpt-web-core-mutation-atomicity.md`.
- No Room Finish mutation/regeneration lane reserved by `2026-08-11-chatgpt-web-gpt56sol-room-finish-mutation-safety.md`.
- No Curtain panel topology, clipping, ownership or P01-P12 implementation; this claim only makes existing Curtain output consume the shared vertical placement.
- No `LOCAL-001` baseline expansion or `LOCAL-004` `QS3DSYNCSOURCE` qualification/implementation.
- No B4D/ED2/proxy parity, physical L/T/X junction output, polygon rebar topology, standard-specific fabrication rules, licensing, signing, installer, release or GitHub Actions dispatch.
- No standalone QS3D executable or AutoCAD adapter work.

## Validation plan

- Re-fetch current `main` before each write and before integration; inspect concurrent changes for every touched builder.
- Run existing Level reference smoke/preflight plus focused new guards for native shared-service consumption, legacy compatibility, full dependent-family coverage and fail-closed unsupported states.
- Run Core Release build/smoke and aggregate local preflights on the exact candidate SHA.
- Compile `QS3D.BricsCAD.V25` x64/Release against the installed BricsCAD V25 managed assemblies.
- Use disposable mm and m drawings to verify legacy/no-Level, Bottom-only, Bottom+Top, Top-only refusal, missing/deleted/ambiguous Level refusal, Level edit invalidation, host-opening-Curtain-rebar Z alignment, Undo, save/reopen and multi-DWG isolation.
- Keep native/private evidence under gitignored `artifacts/`; commit only a sanitized exact-SHA summary. Do not claim `LOCAL_PASS` until the exact runtime matrix passes.

## Coordination

This is a deliberately broad but single coherent vertical-placement lane because splitting hosts from their dependents would create semantic/native divergence. The active neighboring claims visible immediately before publication cover Create Similar, Workspace policy, modeless viewer identity, scoped Core atomicity and Room Finish; none owns native Level placement. Agents may continue those lanes and unrelated Curtain topology, but should not independently wire `BottomLevelId` / `TopLevelId` into native builders or Level assignment UI while this claim is `ACTIVE`.

If a concurrent commit touches a required builder for another feature, this agent will re-read and preserve that implementation, limiting changes to vertical-placement consumption and its tests.

## 2026-08-11 bounded exact-V25 compile prerequisite expansion

Baseline observed before expansion: `origin/main@11486e0726727269df821603d12f202ebd56b412`.

The exact-V25/Core gate is blocked before any Level-owned source compiles by two released-lane compatibility defects already present on current `main`. This claim therefore reserves only the minimum behavior-preserving repairs needed to unblock LOCAL-003 qualification:

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`: make the already-required `name` attribute non-null to nullable flow analysis after the existing empty/whitespace rejection; do not change QSDB validation semantics.
- `src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs`: replace `double.IsFinite(...)`, which is unavailable to the repository's `netstandard2.0` target, with equivalent NaN/Infinity checks; do not change Curtain geometry, bounds or exception policy.
- After those two blockers were removed, the same strict Core gate exposed released Revision-payload nullable-flow errors at `origin/main@9c263d0c5454c5e0f4de06f6a7c7d57d6e1ca658`. Reserve `src/QS3D.Core/Revisions/RevisionService.cs` and `src/QS3D.Core/Revisions/QuantityRevisionReport.cs` only to make the already-required non-blank identity explicit before `Trim()`; preserve the exact canonical-identity validation and exception behavior.
- After the Revision fixes, the strict smoke-project compile exposed one released test-only nullable warning in `tests/QS3D.Core.SmokeTests/RebarNotationWhitespaceRegressionSmoke.cs`. Reserve only the behavior-identical `Nullable<T>.GetValueOrDefault()` access after the existing `HasValue` short-circuit; do not change parser expectations or production rebar code.
- The next Core smoke execution exposed a stale assertion in `tests/QS3D.Core.SmokeTests/DependencyHealthSmoke.cs`: current released source intentionally emits `DEPENDENCY_TARGET_MISSING`, while the old smoke still required no issue. Reserve only alignment to require that exact missing-target issue once and continue rejecting any `DEPENDENCY_CYCLE`; do not change production dependency diagnostics.
- The next V25 adapter compile exposed one released Grid syntax typo in `src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs`: `KeyValuePair<string, ObjectId>>`. Reserve only removal of the extra `>`; do not change Grid annotation behavior, ownership, transactions or audit revision semantics.
- validate with the existing focused smokes/preflights plus Core Release/smoke and V25 x64 Release build. Add only a narrowly necessary regression token if the current gates do not protect target-framework compatibility.

The earlier claims for these exact surfaces are `COMPLETED` and released; current ACTIVE/BLOCKED claim audit found no owner for any of the seven files. This expansion excludes any broader persistence, Revision semantics, rebar/parser behavior, dependency diagnostics, Grid behavior, Curtain topology, geometry-planning or refactor work. The prerequisite fixes will be committed with the coherent LOCAL-003 implementation batch, not as an unrelated feature stream.

## Completion condition

The complete shared Level Z-chain is integrated on current `main`, deterministic tests/static guards pass, the exact-V25 build and required sanitized runtime matrix are recorded, `LOCAL-003` status/evidence is updated truthfully, no dependent family remains on a conflicting Z calculation, and this claim is marked `COMPLETED` with exact pushed SHA(s).

## 2026-08-12 final exact-V25 gate reconciliation expansion

Baseline audited before this expansion: `origin/main@c324c7e8`. The Level-owned Core focused smoke passes, and the Level source itself contributes no remaining adapter compiler error. However, the required whole-project Core/V25 gates are still blocked by released remote lanes that were merged without local compiler/runtime execution. The earlier owners of every exact surface below are `COMPLETED` or `RELEASED`; the current ACTIVE/BLOCKED audit found no other agent reserving these exact repairs. The separate active local BQ preflight claim belongs to the same `codex-local-019ff0c5` identity and explicitly excluded the product-source changes while their former owners were active.

Reserve only the following behavior-preserving gate reconciliation needed to produce and qualify one exact-SHA Level candidate:

- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsCloneValidationSmoke.cs`: replace two invalid `const string` numeric concatenations with immutable runtime strings; preserve messages and cardinality expectations.
- `src/QS3D.BricsCAD.V25/Updates/SemanticReleaseVersion.cs` and `Updates/UpdateManifestProbe.cs`: make the already-validated non-null string flow explicit without nullable suppression; preserve SemVer, tag and manifest policy.
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs` and `WorkspacePanel.DarkContextMenu.cs`: expand invalid two-argument WPF `Thickness` calls to the equivalent horizontal/vertical four-side values and make existing optional UI references explicit; no layout redesign or handler change.
- `src/QS3D.BricsCAD.V25/UI/RightPanel.CompactShell.cs` and `RightPanel.XrefLock.cs`: preserve optional compact controls and qualify the intended BricsCAD `Application`; no Xref/layer behavior change.
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs` and `WallQuantityWindow.xaml.cs`: make already-guarded optional row/project lookups explicit to nullable analysis; no quantity, locate, modeless-affinity or export behavior change.
- `tests/QS3D.Core.SmokeTests/DoorOpeningXlsxSmoke.cs` and `MaterialUsageXlsxSmoke.cs`: align stale invalid-XML-character assertions with the released exporter contract that sanitizes to U+FFFD before writing valid worksheet XML; continue proving the control character is absent.
- `tests/QS3D.Core.SmokeTests/GeneratedRebarProviderOwnershipSmoke.cs`: align the stale null-element assertion with the released generated-handle ownership index contract that rejects malformed null semantic entries fail-closed.
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` plus validation-only `tests/QS3D.Core.SmokeTests/ScheduleReportingIdentitySmoke.cs`: return the canonical project Family identity after case-insensitive lookup instead of the caller's trimmed casing; preserve grouping, quantity arithmetic and immutable input state.

Do not broaden this expansion into updater policy, Workspace/Right Panel design, quantity arithmetic, XLSX format changes, generated ownership semantics or reporting redesign. Re-run full Core smoke and installed-V25 `Release|x64` after these exact repairs. If a further unrelated blocker appears, diagnose it first and update the published claim again before editing any additional surface.

## 2026-08-12 quantity revision smoke reconciliation expansion

Baseline audited before this expansion: `origin/main@93aacb0e`. The full Core smoke now reaches one stale assertion in `tests/QS3D.Core.SmokeTests/QuantityReportRevisionReviewSmoke.cs`: it attempts to capture `-double.MaxValue` and then exercise subtraction overflow, but the completed reporting integrity contract now rejects every negative physical quantity during capture. With two valid finite non-negative snapshots, their signed difference cannot overflow.

Reserve only that smoke file to replace the unreachable overflow setup with a direct fail-closed negative-quantity capture assertion while retaining the existing non-finite assertion. Do not edit `QuantityReportRevisionService`, `RevisionService`, `RevisionMath`, production quantity validation or comparison semantics. The concurrent ACTIVE revision-ID and dependency-canonicalization claims remain fully excluded. Re-run the complete Core smoke after this narrow reconciliation.

## 2026-08-12 semantic-number hardening smoke reconciliation expansion

Baseline audited before this expansion: `origin/main@67e822d0`. The next full Core smoke reaches a stale assertion in `tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs`: it expects a present `LengthM=NaN` value to regenerate as zero, but completed semantic-number hardening now requires every present malformed/non-finite numeric property to fail closed while only a missing property may use its fallback.

Reserve only that smoke file to require `WallRegenerator.Regenerate(...)` to throw for the present NaN value and to prove no derived wall quantities were partially written. Preserve the remaining finite clamping assertions in the same smoke. Do not edit `SemanticNumber`, regenerators, quantity math or the dedicated semantic-number regression; their released production contract remains authoritative. Re-run the complete Core smoke after this narrow reconciliation.

## 2026-08-11 source-safe wave heartbeat

- Synced baseline: `origin/main@e085c82732d80eb25ba3dcb719715d6ca077b37f` before final validation.
- Implemented: geometry-driving Level-key invalidation, transitive dependent stale propagation, pure effective-span preparation, and the first wall/structural native host adapter wave.
- Safety boundary: native mutation and production quantity regeneration reject configured Level references through `LevelReferenceNativeIntegrationPolicy.EnsureQualified(...)`; the policy still qualifies no category.
- Automated exact-SHA evidence: `36c170dcaf75e0018e3370a42978e63849530602` passed 453/453 aggregate gates, Core Release/smoke, adapter V25 x64 Release, offline WPF, and licensed V25 NETLOAD/Ribbon/Palette runtime. Plugin SHA-256: `762e28dafb1ac9427602efdf032f2fd6cc6e7511e2869f4861c9d52b30d1bcc7`.
- Qualification boundary: `FULL INTERACTIVE/PRIVATE-DWG PRODUCT MATRIX = NOT_RUN`; customer release qualification remains false. This automated baseline does not qualify Level Z geometry.
- Remaining: straight/curved opening cutters, AutoHost, Curtain LINE/path frame/panel/live fingerprints, generated rebar/mesh/shape families, UI, and the complete mm/m V25 runtime matrix.
- Claim remains `ACTIVE`; LOCAL-003 remains `OPEN / PENDING_LOCAL`.

## 2026-08-11 hosted-opening source wave heartbeat

- Synced implementation baseline: `origin/main@b45f416909bf246eebbc064b3ab75384778719e6`.
- Implemented a shared hosted-opening placement result in `ElementVerticalPlacementService`, then consumed it in straight LINE/open-POLYLINE cuts, curved/bulged cuts, the physical-cut live fingerprint, and Auto Host elevation matching.
- Legacy boundary: when neither host nor opening has a configured Level reference, the existing source-relative Z arithmetic and serialized cut fingerprints remain byte-for-byte on their prior path. Level-derived fingerprint tokens are emitted only for configured Level placement.
- Safety boundary: `LevelReferenceNativeIntegrationPolicy.IsQualified(...)` still returns `false` for every category. Configured Level references therefore fail before Boolean subtraction, physical-cut metadata writes, or Auto Host link mutation; this wave does not expose partial Level authoring in the UI.
- Focused evidence on the synchronized working tree: dedicated Level smoke PASS; new `scripts/preflight-level-opening-placement.py` PASS; existing straight/curved/incremental/targeted/live-health/rehost/AutoHost gates PASS; Core Release and SmokeTests projects compile with zero warnings/errors.
- Whole-tree qualification is not claimed: aggregate preflight and adapter V25 build still have failures in concurrently owned Plan-to-3D, Right Panel, updater, Workspace and quantity-reporting surfaces. No Level/opening compiler or focused-gate failure was observed.
- Remaining: Curtain host/frame/panel/live-state placement, generated rebar/mesh/shape placement, final policy/UI enablement, and the complete exact-SHA mm/m BricsCAD V25 runtime matrix.
- Claim remains `ACTIVE`; LOCAL-003 remains `OPEN / PENDING_LOCAL`.

## 2026-08-11 full-chain source candidate heartbeat

- Synchronized source baseline before this heartbeat: `origin/main@15c80902c6067f222cb6d4764b4b089079de67cf`.
- The coherent source candidate now routes native hosts, hosted straight/curved opening cuts and Auto Host, Curtain LINE/path frame/panel/live-state output, generated longitudinal/tie/stirrup/mesh/shape reinforcement and effective semantic quantities through one branch-lazy Level placement contract.
- Generated vertical snapshots and Level-edit stale propagation cover the integrated dependent families. Guarded Bottom/Top/Clear actions are present in the Floor/Level modeless UI and preserve the bound-project identity guard.
- Source-enabled policy categories are ArchitecturalWall, GlassWall, WallPier, StructuralWall, Beam, Slab, Column, Foundation, Stair, Railing, Door and WallOpening; unsupported categories remain fail-closed.
- Focused static Level/opening/rebar/Floor stale-project gates pass on the synchronized working tree. The aggregate Core/V25 build is currently blocked before Level compilation by unrelated current-main errors in `QsdbProjectXmlSchemaValidator.cs` and `CurtainFrameOpeningPlanner.cs`; no Level-owned compiler error has been observed.
- `QS3DLEVELZPROBE` and `scripts/test-bricscad-v25-level-z.ps1` are prepared for a clean exact-SHA candidate. No result is claimed yet: focused native probe, complete mm/m/full-family/Undo/save-reopen/multi-DWG/private matrix and customer-release qualification remain `PENDING_LOCAL / NOT_LOCAL_PASS`.
- Claim remains `ACTIVE`; LOCAL-003 is `IN_PROGRESS` and must not be closed until exact-SHA evidence is recorded.
