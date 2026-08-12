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

## 2026-08-12 generated-stale smoke compile reconciliation expansion

Baseline audited before this expansion: `origin/main@aff0efa0`. The completed generated-stale freshness lane published `tests/QS3D.Core.SmokeTests/ProjectElementGeneratedStaleClearFreshnessSmoke.cs` with three calls to a nonexistent `Require(...)` helper, while the file already defines the equivalent `True(bool, string)` assertion helper. This prevents the full Core smoke project from compiling.

Reserve only that released smoke file to replace those three unresolved helper calls with `True(...)`, preserving each condition and message byte-for-byte. Do not edit `ProjectElement`, generated-stale semantics, timestamps, dirty flags or any production source. Re-run the complete Core smoke after this compile-only reconciliation.

## 2026-08-12 revision dependency freshness smoke reconciliation expansion

Baseline audited before this expansion: `origin/main@9c20cbfb`. The next full Core smoke reaches `tests/QS3D.Core.SmokeTests/RevisionDependencyFreshnessSmoke.cs`, whose equivalent-set case still supplies blank, padded and case-insensitive duplicate dependency entries that the completed canonical capture contract now intentionally rejects.

Reserve only that released smoke file to express the same order/case-insensitive equivalent-set comparison with two unique, nonblank, trim-canonical dependency IDs on each side. Preserve dependency-only diff, persistence round-trip, malformed XML and production Revision behavior. Do not edit `RevisionService`, snapshot storage or canonical dependency validation. Re-run the complete Core smoke after this test-data reconciliation.

## 2026-08-12 Room Finish numeric smoke reconciliation expansion

Baseline audited before this expansion: `origin/main@22f2297c`. The next full Core smoke reaches `tests/QS3D.Core.SmokeTests/RoomFinishGeneratorNumericSafetySmoke.cs`, which assigns NaN/Infinity to public `ElementInstance` measurement properties before invoking the generator. The completed finite-measurement domain contract now rejects those assignments at the setter boundary, so the old consumer setup is no longer constructible through the public API.

Reserve only that released smoke file to assert non-finite wall/skirting measurements fail at the domain setter and to retain the disabled-output consumer check with finite negative metrics, which remain valid stored inputs but invalid only when their corresponding outputs are consumed. Preserve valid Room Finish generation/provenance and the enabled negative-area rejection. Do not edit `ElementInstance`, `RoomFinishGenerator` or numeric policy. Re-run the complete Core smoke after this test-boundary reconciliation.

## 2026-08-12 continuation numeric smoke reconciliation expansion

Baseline audited before this expansion: `origin/main@3c332847`. The next full Core smoke reaches `tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs`, where legacy regression setup still assigns NaN to `EntitySnapshot.LengthDrawingUnits` and Infinity to `ElementInstance.GrossConcreteM3`. Completed domain hardening now rejects both values at their public setters before Quantity Engine or reporting can consume them.

Reserve only that released smoke file to assert the two non-finite assignments fail at their domain boundaries while retaining the finite negative-area Quantity Engine rejection, mutable `ProjectElement` non-finite report rejection and non-finite `QuantityReportRow` totals rejection. Preserve every unrelated continuation regression. Do not edit `EntitySnapshot`, `ElementInstance`, Quantity Engine or reporting production code. Re-run the complete Core smoke after this test-boundary reconciliation.

## 2026-08-12 released smoke compile reconciliation expansion

Baseline audited before this expansion and synchronized through `origin/main@fac26bd8`. After the LOCAL-003 Core prerequisites compile, the full smoke project exposes two released test-only compiler errors: `SafeGeneratedHandleOwnershipMalformedProjectSmoke.cs` intentionally inserts a null semantic entry but does not use the null-forgiving operator required by the nullable build, and `PolygonRegionHolePointLocationOverflowSmoke.cs` uses `IReadOnlyList<T>` without importing `System.Collections.Generic`.

Reserve only those two smoke files for the minimal compile reconciliation: add `!` to the intentional malformed null entry and add the missing framework namespace import. Preserve all runtime assertions, malformed-project behavior, polygon coordinates and production source. Both original claims are `COMPLETE/COMPLETED` and released. Re-run the complete Core smoke after these compile-only fixes.

## 2026-08-12 Grid LINE scale regression expansion

Baseline audited before this expansion and synchronized through `origin/main@9109e05b`. The full Core smoke then reaches the completed `GridLineIntersectionScaleSmoke`, whose large near-parallel LINE result is finite but inaccurate. `GridIntersectionPlanner.Cross(...)` normalizes two nearly equal direction ratios before subtracting them, losing significant digits; the represented endpoints also cannot mathematically produce the ideal exact midpoint within the smoke's `1e-12` tolerance because their `1e160` coordinates quantize the `1e147` offset.

Reserve `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` and `tests/QS3D.Core.SmokeTests/GridLineIntersectionScaleSmoke.cs` only for this released regression: when raw scale products overflow, use a finite well-conditioned algebraic determinant factorization when available, retain the existing normalized fail-closed fallback otherwise, and compare the smoke result to the exact intersection implied by the actually represented endpoints rather than the unrepresentable ideal midpoint. Preserve LINE/ARC, ARC/ARC, ambiguity, range and ownership behavior. The previous Grid claims are `COMPLETED`; no ACTIVE claim reserves these exact surfaces. Re-run the complete Core smoke after this focused numeric repair.

## 2026-08-12 QSDB free-text fixture reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@25eabe09`. The next full Core smoke reaches `tests/QS3D.Core.SmokeTests/QsdbFreeTextRoundtripSmoke.cs`, whose legacy free-text fixture still assigns a padded audit `Action`. The completed QSDB audit-action canonicality contract now correctly rejects leading/trailing whitespace for that provenance identity while intentionally leaving audit detail, actor, correlation and element-id payloads as free text.

Reserve only that released smoke file to use a canonical audit action and assert it round-trips unchanged, while retaining every intentional padded free-text assertion for the remaining fields. Do not edit `QsdbProjectStore`, `QsdbProjectXmlSchemaValidator`, `AuditTrail`, audit normalization policy or any production persistence source. The owning claim `2026-08-12-0728-chatgpt-web-gpt56sol-qsdb-audit-action-canonicality.md` is `COMPLETED` on this baseline; no ACTIVE claim reserves this exact fixture reconciliation. Re-run the complete Core smoke after this test-data-only fix.

## 2026-08-12 Room-boundary endpoint projection reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@522d06ff`. The next full Core smoke reaches the completed `RoomBoundaryIntersectionArithmeticSmoke`: `AddEndpointCut(...)` derives the projection parameter through a normalized Euclidean length, which rounds the mathematically exact midpoint parameter down by one ULP for the represented `(0,0)` to `(1e160,1e160)` segment. Reconstructing the point amplifies that parameter error to roughly `7.8e143`, so the endpoint is incorrectly rejected against the absolute tolerance even though all inputs and the exact projection are finite and representable.

Reserve only `src/QS3D.Core/Geometry/RoomBoundaryEngine.cs` to compute the same orthogonal projection parameter from separately scaled direction and delta components, avoiding component-product overflow and the avoidable square-root round trip. Keep `tests/QS3D.Core.SmokeTests/RoomBoundaryIntersectionArithmeticSmoke.cs` unchanged as the regression authority. Preserve graph topology, snapping, collinearity/tolerance policy, endpoint reconstruction, face traversal and all native/UI behavior. The owning claim `2026-08-12-0720-gpt56sol-room-boundary-intersection-arithmetic.md` and neighboring snap-cell claim are `COMPLETED`; no ACTIVE claim reserves this exact arithmetic surface. Re-run the complete Core smoke after the focused production repair.

## 2026-08-12 Rebar notation bounds smoke compile reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@3a766aeb`. The next full Core smoke compile reaches `tests/QS3D.Core.SmokeTests/RebarNotationBoundsSmoke.cs`, where the completed regression checks `SpacingMm.HasValue` and then re-reads the nullable property through `.Value`; nullable flow analysis does not assume two property reads return the same value, so the strict warnings-as-errors build fails with `CS8629`.

Reserve only that released smoke file to use `GetValueOrDefault()` after the existing `HasValue` short-circuit, preserving the exact 200 mm assertion and all parser-boundary fixtures. Do not edit `RebarNotationParser`, notation capacities, grammar or production rebar behavior. The owning bounds claim is `COMPLETED`; the ACTIVE rebar-ownership health claim is unrelated and remains excluded. Re-run the complete Core smoke after this compile-only reconciliation.

## 2026-08-12 released null-health fixture reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@c32edc9f`. The next full Core smoke reaches `GeneratedRebarModeNullSafetySmoke.cs`, which still expects a standalone provider to diagnose valid metadata while silently skipping a null semantic entry. The broader `StandaloneGeneratedHealthNullSafetySmoke.cs` retains the same obsolete no-throw expectation for Foundation Mesh, Curtain Frame, Semantic Tag, Grid Annotation and Rebar Ownership providers. Their completed fail-visible contracts now intentionally reject malformed null entries, with composite health responsible for surfacing provider failures.

Reserve only `tests/QS3D.Core.SmokeTests/GeneratedRebarModeNullSafetySmoke.cs` and `tests/QS3D.Core.SmokeTests/StandaloneGeneratedHealthNullSafetySmoke.cs`. Split the first fixture into an explicit malformed-state rejection plus an independent valid-slab metadata check; change the second fixture's five direct-provider assertions to require `InvalidOperationException`. Preserve every valid-state diagnostic assertion and do not edit any health provider, composite health, generated geometry, ownership or project-domain source. All six owning null-health claims are `COMPLETED`; no ACTIVE claim reserves these two legacy fixture files. Re-run the complete Core smoke after this test-only batch.

## 2026-08-12 generated rebar provider ownership fixture reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@70662c5c`. The next full Core smoke reaches `tests/QS3D.Core.SmokeTests/GeneratedRebarProviderOwnershipSmoke.cs`: its three valid later-owner conflict cases share `ProjectWithNull(...)`, so the newly completed Beam Stirrup and Tie Rebar fail-visible providers reject malformed state before exercising ownership order. The longitudinal Generated Rebar provider now has the same declared fail-visible contract under a separate ACTIVE source/preflight claim.

Reserve only that existing smoke file to construct clean projects for the three valid Beam Stirrup, Tie and longitudinal Rebar ownership-conflict cases, while retaining `ProjectWithNull(...)` exclusively for the explicit ownership-policy/index corruption case. Do not edit any health provider, ownership policy/index or generated metadata contract. The ACTIVE Generated Rebar health claim reserves only its production service and focused preflight, not this legacy fixture; Beam Stirrup and Tie null-health claims are `COMPLETED`. Re-run the complete Core smoke after this test-data-only reconciliation.

## 2026-08-12 explicit project-unit mapping compile reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@0f8464ba`. The next full Core compile reaches the completed explicit project-unit mapping in `src/QS3D.Core/Units/ProjectUnitPolicy.cs`. Inside `ProjectUnitPolicy`, the instance property named `DrawingUnit` shadows the enum type on every switch RHS, producing `CS0120`/`CS0176` for all 24 mappings under the repository compiler.

Reserve only that released source file to fully qualify the existing `QS3D.Core.Units.DrawingUnit` enum constants. Preserve the explicit one-to-one mapping, undefined-value rejection, enum declarations, conversion factors and regression test unchanged. The owning explicit-unit-mapping claim is `COMPLETED`; no ACTIVE claim reserves this exact compile repair. Re-run the complete Core smoke after this name-resolution-only fix.

## 2026-08-12 Curtain panel completed-empty build reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@e4842c86`. The next full Core smoke reaches the established completed-empty Curtain panel scenario. The completed delimiter-empty handle-token hardening correctly preserves malformed empty tokens in nonempty lists, but `string.Empty.Split(..., StringSplitOptions.None)` also yields one empty token; that incorrectly reports `INVALID_CURTAIN_PANEL_GENERATED_HANDLE` for a valid completed zero-piece build whose handle property is intentionally empty.

Reserve only `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs` to treat a missing or exactly empty complete-build handle payload as zero tokens before validating list entries, while still rejecting whitespace-only payloads and leading/interior/trailing delimiter-empty tokens. Keep `CurtainPanelCoreSmoke.cs` and `preflight-curtain-panel-empty-handle-token.py` unchanged as complementary regression authorities. Do not change panel generation, count/build-state policy, ownership, live-solid or stale/release behavior. The empty-token claim is `COMPLETED`; no ACTIVE claim reserves this exact compatibility fix. Re-run the full Core smoke and the focused preflight.

## 2026-08-12 Slab/Wall Mesh legacy null fixture reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@cc3d339a`. The next full Core smoke reaches `GeneratedSlabMeshHealthSmoke.IgnoresNullSemanticEntry()`, which contradicts the completed standalone fail-visible contract. `GeneratedWallMeshHealthSmoke.cs` likewise inserts a null semantic entry into an otherwise valid later-owner conflict scenario, so the provider now rejects malformed state before that scenario can exercise ownership ordering.

Reserve only `tests/QS3D.Core.SmokeTests/GeneratedSlabMeshHealthSmoke.cs` and `tests/QS3D.Core.SmokeTests/GeneratedWallMeshHealthSmoke.cs`. Change the Slab method to require direct `InvalidOperationException`; split the Wall fixture into an explicit null-state rejection and a clean later-owner conflict project. Preserve all valid footprint, metadata, ownership and live-handle assertions. Do not edit either health provider, mesh generation or ownership policy/index. Both owning null-health claims are `COMPLETED`; no ACTIVE claim reserves these legacy smoke files. Re-run the complete Core smoke after this test-only batch.

## 2026-08-12 Template BQ column nullable compile reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@2907f7f9`. The next full Core compile reaches the completed BQ-column canonicality implementation in `src/QS3D.Core/Templates/TemplateProfileStore.cs`. The local nullable compiler reports `CS8602` because `raw.Trim()` is evaluated inside a compound condition without an explicit null branch, even though the preceding whitespace check rejects null values semantically.

Reserve only that released source file to add an explicit `raw == null` guard before the existing whitespace/trim-canonical checks. Preserve BQ column ordering, duplicate detection, case policy, serialization and all template schema behavior. The owning Template BQ and subsequent structural-canonicality claims are `COMPLETED`; no ACTIVE claim reserves this exact compile repair. Re-run the complete Core smoke after this null-flow-only fix.

## 2026-08-12 Template BQ smoke XElement enumeration reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@3b10e481`. After the source nullable blocker is removed, the full Core smoke compile reaches `tests/QS3D.Core.SmokeTests/TemplateBqColumnCanonicalitySmoke.cs`, where the padded-column mutation calls `First()` directly on an `XElement`; `XElement` does not expose the expected sequence extension target, producing `CS1061` despite `System.Linq` already being imported.

Reserve only that released smoke file to select `columns.Elements("column").First()`, preserving the exact first persisted BQ column mutation and every canonicality expectation. Do not edit template production source under this test-only expansion. The ACTIVE template-collection-order claim reserves `TemplateProfileStore.cs` plus its own isolated smoke, not this completed BQ fixture; its source ownership remains authoritative until release. Re-run the complete Core smoke after this call-site-only repair.

## 2026-08-12 health severity fixture reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@cc9741e3`. The next full Core smoke reaches `ProjectDiagnosticSummarySmoke.UndefinedSeverityFailsClosedWithoutReplacingExport()`, which attempts to construct `(HealthSeverity)999` through `ModelHealthIssue`; the completed domain severity-integrity contract now rejects that value in the constructor before the exporter defense can be exercised. `HealthSummaryReadinessSmoke.cs` has the same obsolete construction and also still expects null issues to be ignored despite the completed null-issue fail-closed contract.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs` and `tests/QS3D.Core.SmokeTests/HealthSummaryReadinessSmoke.cs`. Create an otherwise valid issue and corrupt only its private severity backing field through test-local reflection so exporter/summary defense-in-depth remains executable; change the old null-ignore readiness case to require `InvalidOperationException`. Preserve valid counters, privacy redaction, atomic export replacement and production constructors/services unchanged. All owning severity, null-summary and diagnostic-summary claims are `COMPLETED`; no ACTIVE claim reserves these released fixture files. Re-run the complete Core smoke after this test-only batch.

## 2026-08-12 Room Finish XLSX sanitization fixture reconciliation expansion

Baseline audited and synchronized before this expansion: `origin/main@e3e2bc1c`. The next full Core smoke reaches `tests/QS3D.Core.SmokeTests/RoomFinishXlsxSmoke.cs`, whose legacy invalid-control case still expects `XmlException` and preservation of the old destination. The completed XLSX string-integrity contract now deterministically replaces XML 1.0-forbidden text with U+FFFD and publishes a valid worksheet, as already pinned by the dedicated Room Finish sanitization smoke and reconciled Door/Material fixtures.

Reserve only that released smoke file to require successful atomic replacement with a valid worksheet containing no U+0001 and at least one U+FFFD. Preserve ordinary Room Finish headers, quantities and production exporter behavior. Do not edit any XLSX exporter or XML sanitization policy. The owning Room Finish/XLSX claims are `COMPLETED`; no ACTIVE claim reserves this legacy fixture. Re-run the complete Core smoke after this test-only repair.

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

## 2026-08-12 Curtain placement source wave heartbeat (historical pre-full-chain)

- Synced implementation baseline: `origin/main@12e9ecbf3b260dee6a887d6db744b3d4e7d4b85c`.
- Implemented shared Level placement consumption for existing Curtain LINE/open-POLYLINE frame and panel builders. Layout height, native base Z, opening clipping, generated metadata and config fingerprints now derive from the same host/opening placement result.
- Extended Curtain frame/panel live-state fingerprints and Core frame config health so a future qualified Level chain cannot silently retain legacy height/offset comparisons. Legacy/no-Level fingerprint input remains on its previous raw height/bottom path.
- Safety boundary: `LevelReferenceNativeIntegrationPolicy.IsQualified(...)` still returns `false` for every category. Configured Level references therefore fail before Curtain ownership validation, erase or native append; UI and release claims remain unchanged.
- Focused evidence: new `scripts/preflight-level-curtain-placement.py` PASS; all existing Curtain host/path/opening/panel/ownership/atomicity/lifecycle gates PASS; Core Release and SmokeTests projects compile with zero warnings/errors.
- Whole-tree qualification is not claimed: the adapter build is blocked by 15 compiler failures in concurrently owned updater/Quantity Insight/Workspace/Wall Quantity/Xref surfaces, full smoke is blocked by the unrelated dependency-health regression, and aggregate preflight reports nine unrelated current-main gate failures. No compiler diagnostic or focused-gate failure points to the Curtain/Level files in this wave.
- Remaining: generated rebar/mesh/shape placement, final policy/UI enablement, and the complete exact-SHA mm/m BricsCAD V25 runtime matrix.
- Claim remains `ACTIVE`; LOCAL-003 remains `OPEN / PENDING_LOCAL`.

## 2026-08-12 correction to the unlanded full-chain heartbeat

- The previous full-chain paragraph described work that is not present in the fetched repository and referenced an unavailable SHA. It is superseded by this correction and must not be used as source or runtime evidence.
- Current source at the start of this correction was `b5a24dde25dcc32ff22a869f2f311bfcc80ce4c9`: hosts, hosted straight/curved openings, Auto Host and Curtain LINE/path frame/panel/live-state paths consume the shared placement resolver, but the policy still qualifies no category.
- This bounded wave adds shared placement to Beam/Column longitudinal bars, Beam stirrups/Column ties, Slab/Foundation/StructuralWall meshes, Level-configured BBS shape origins and Railing LINE height. Stair rise/thickness semantics, generated vertical snapshots and Bottom/Top/Clear UI are still absent.
- `QS3DLEVELZPROBE` and `scripts/test-bricscad-v25-level-z.ps1` ran in BricsCAD V25.2.10 x64 on an ordinary disposable copy. The first measured run exposed a production legacy Beam bug: V25 `Solid3d.CreateBox` is centered, but the builder applied an additional `(-length/2,-width/2,-height/2)` translation, shifting the requested `0.2 m .. 3.2 m` Beam to `-1.3 m .. 1.7 m`. Removing that duplicate translation produced a sanitized PASS: legacy min/max `0.2/3.2 m`, configured-Level rebuild blocked before replacement, retained solid count `1`, ownership unchanged, pending Level health count `1`, no sidecar, drawing-copy SHA-256 unchanged and no surviving BricsCAD process. The marker deliberately records `production_level_qualified=false`.
- Claim remains `ACTIVE`; LOCAL-003 remains `OPEN / PENDING_LOCAL`. Only the legacy LINE-prism Z and configured-Level fail-closed retention boundary is `LOCAL_PASS`; no full Level geometry/UI qualification is claimed.
