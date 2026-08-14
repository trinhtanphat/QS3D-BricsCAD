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

## 2026-08-12 Family global-duplicate smoke compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@ae8f98a96b400d908acfde2629274fafcc2a6eff`.

The required full Core gate cannot compile because the completed Family global-duplicate regression constructs `ProjectElement` twice with the obsolete three-argument call, while the current domain constructor requires `id`, `category`, `familyId`, `floorId` and `zoneId`. Both owning Family claims are `COMPLETED` / `RELEASED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectFamilyGlobalDuplicateIntegritySmoke.cs` to pass empty Floor/Zone IDs in those two test fixtures. Preserve duplicate-family rejection, valid-operation coverage, all production Family/domain behavior and every assertion unchanged. Re-run the full Core smoke after this compile-only reconciliation. Do not edit `ProjectElement`, `ProjectFamilyService`, Family UI or persistence under this expansion.

## 2026-08-12 reporting/selection null-flow compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@c4a3e44b2552848b867e4ad977fe36957cd4f6d8`.

The required full Core gate still cannot compile after the earlier nullability repairs because later completed canonicality/reference-integrity lanes introduced three new nullable-flow diagnostics: `ReportingProjectIdentityGuard.RequireExistingReference(...)` passes a nullable value to `ISet<string>.Contains(...)`, `RequireCanonicalReference(...)` trims the same nullable value, and `SemanticSelectionInspector.CanonicalOptionalReference(...)` trims a nullable value after its existing blank guard. Runtime behavior already rejects/returns before all three dereferences; only the local warnings-as-errors compiler remains unconvinced. Every owning reporting/selection claim is `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for either exact file.

Reserve only `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs` and `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` to make those three already-guarded values explicitly non-null at their existing use sites. Preserve reference existence/canonicality, error text, selection freshness, all reporting/selection output and every test unchanged. Do not broaden this expansion into reporting identity policy, semantic selection behavior or new tests. Re-run the complete Core smoke after this nullable-flow-only reconciliation.

## 2026-08-12 released smoke API reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@f66500aec20a0704bbda4e6459829643a243ee35`.

After the preceding nullable-flow blockers are removed, the full Core smoke project exposes eight compile errors in four completed-lane fixtures. `SemanticSelectionRelationCanonicalitySmoke.cs` and `SemanticSelectionInspectorInputFreshnessSmoke.cs` still use the removed generic `ElementCategory.Wall` member instead of the current `ArchitecturalWall` category. `ProjectFloorGlobalNullIntegritySmoke.cs` and `ProjectFamilyGlobalNullIntegritySmoke.cs` still call obsolete shortened `ProjectElement` constructors and omit the now-required Zone or Floor/Zone relation arguments. All four owning claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for these exact fixtures.

Reserve only those four smoke files for mechanical current-API alignment: replace `ElementCategory.Wall` with `ElementCategory.ArchitecturalWall`; append an empty Zone ID to the two Floor fixtures; append empty Floor and Zone IDs to the two Family fixtures. Preserve every malformed-null setup, relation/canonicality value, operation and assertion unchanged. Do not edit production selection, Family, Floor, domain constructor or category policy. Re-run the complete Core smoke after this test-only compile reconciliation.

## 2026-08-12 bulk Family canonical no-op fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@fb0390b2969662348df1293cb16eb715c0621904`.

The full Core smoke next reaches `BulkFamilyCanonicalNoOpSmoke.SelectionBulkAssignmentReportsCanonicalNoOp()`. The completed selection relation-canonicality contract now rejects its deliberately padded stored `FamilyId` during `SemanticSelectionInspector` validation, before the fixture can exercise the independently valid invariant that a padded/case-varied requested target is a canonical no-op. The direct `BulkEditService` case remains valid and continues to pin its existing padded stored-reference behavior. The fixture's owning bulk-Family claim and the later selection-canonicality claim are both `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact smoke file.

Reserve only `tests/QS3D.Core.SmokeTests/BulkFamilyCanonicalNoOpSmoke.cs` to give the semantic-selection case a canonical stored `FamilyId` while retaining its padded/case-varied requested target, and make the shared no-mutation assertion compare against each scenario's captured starting `FamilyId` instead of one hard-coded padded value. Preserve changed counts, properties, dirty/timestamp/version state, direct bulk behavior and genuine reassignment behavior. Do not edit either bulk service, the selection inspector or Family identity policy. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 quantity-rule Family fixture constructor reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@da6f7353291252f948b43667076e95d46adb3419`.

The full Core smoke compile next reaches two obsolete three-argument `ProjectElement` calls in the newly completed `QuantityRuleFamilyGlobalIdentitySmoke.cs`. Both fixtures intentionally have no Floor or Zone relation, but the current domain constructor requires those explicit relation arguments. The owning quantity-rule Family identity claim is `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact smoke file.

Reserve only `tests/QS3D.Core.SmokeTests/QuantityRuleFamilyGlobalIdentitySmoke.cs` to append empty Floor and Zone IDs to those two constructor calls. Preserve Family identity/canonicality, malformed-global-state, rollback/no-op, rule result and every assertion unchanged. Do not edit quantity-rule production code, `ProjectElement`, Family policy or add new behavior. Re-run the complete Core smoke after this constructor-only fixture reconciliation.

## 2026-08-12 schedule reference-integrity fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@e172db3499883a470ea42e2135353d8984cacdce`.

The full Core smoke next reaches `CurtainWallScheduleGroupKeyCollisionSmoke`, whose delimiter-collision elements use nonblank Floor/Family references without adding the referenced definitions. The completed shared reporting reference-existence contract now correctly rejects that malformed project before grouping. Static review of the adjacent completed delimiter-collision fixtures found the same incompatibility in `DoorOpeningScheduleGroupKeyCollisionSmoke` (missing Family definitions) and `RoomFinishScheduleGroupKeyCollisionSmoke` (missing Zone definition). Their owning grouping claims and reporting-reference claim are all `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for these exact fixtures.

Reserve only those three smoke files to add the minimum matching project definitions while preserving the delimiter-bearing IDs and visible labels that form each collision tuple: Curtain gets its two Floor and two GlassWall Family definitions, Door gets its two Door Family definitions, and Room Finish gets its referenced Zone. Preserve grouping keys, row counts, quantities, names/materials/provenance and every assertion. Do not edit any reporting builder, identity guard, grouping algorithm or production domain policy. Re-run the complete Core smoke after this fixture-data-only reconciliation.

### 2026-08-14 Curtain collision fixture delegation

Current Floor/Family persistability validation makes U+001F definition identities unreachable. Parent task `/root` explicitly delegates only `tests/QS3D.Core.SmokeTests/CurtainWallScheduleGroupKeyCollisionSmoke.cs` to `/root/fix_level_curtain_frame_z` under `2026-08-14-codex-curtain-schedule-valid-collision-fixture.md`: use valid printable-delimiter identities plus an explicit test-local legacy delimiter-collision assertion. The parent retains LOCAL-003 probe/runner ownership and will not edit this smoke while the child claim is `ACTIVE`.

Completed through PR `#1164` at `8d33c39f9cbd30cf37606c67dba44244cfb843b3`. The registered smoke now advances past the Curtain collision fixture and reaches the separate stale Door collision fixture; no Level or production surface changed.

### 2026-08-14 Door collision fixture delegation

Parent task `/root` explicitly delegates only `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleGroupKeyCollisionSmoke.cs` to `/root/fix_level_curtain_frame_z` under `2026-08-14-codex-door-schedule-valid-collision-fixture.md`: replace unreachable U+001F Family identity data with valid printable-delimiter values and add an explicit test-local legacy delimiter-collision assertion over the existing grouping tuple. The parent retains LOCAL-003 probe/runner ownership and will not edit this smoke while the child claim is `ACTIVE`.

## 2026-08-12 base schedule reference-integrity fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@f74dbc55d5c141b78d7f20d0a65bac26b901126f`.

The full Core smoke next reaches `CurtainWallScheduleSmoke.GroupsByStableFloorAndFamily()`, whose otherwise valid elements reference Zone `z` without defining it. A read-only audit of the adjacent base schedule fixtures found the same completed reporting-reference incompatibility in `DoorOpeningScheduleSmoke.cs`, `MaterialUsageScheduleSmoke.cs` and `RoomFinishScheduleSmoke.cs`; several focused invalid-value cases also omit their referenced Floor/Family/Zone definitions, allowing the shared identity guard to throw before the intended numeric assertion. The owning schedule and reporting-reference claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for these exact four fixtures.

Reserve only those four base smoke files to add the minimum matching Zone and, where currently absent, Floor/Family definitions to each project setup that uses nonblank references. Preserve all existing element relation IDs, inherited/override behavior, valid rows, invalid numeric inputs, grouping, quantity/unit/provenance expectations and exception assertions. Do not edit any schedule builder, identity guard, domain service or production policy. Re-run the complete Core smoke after this fixture-data-only reconciliation.

## 2026-08-12 ElementInstance finite-measurement fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@4874abb8444a7b2caceab068c49679321633f892`.

The full Core smoke next reaches `ElementInstanceFiniteMeasurementsSmoke.cs`, whose older finite-value control still assigns and expects a negative `LengthM`. The completed domain contract now rejects every negative physical measurement at the setter, with dedicated `ElementInstanceNonNegativeMeasurementsSmoke` coverage for that rejection. The older fixture should continue pinning defaults, accepted finite values, non-finite rejection, retained state and net-concrete semantics without contradicting the newer boundary. Its original finite-measurement claim and the nonnegative-measurement claim are `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/ElementInstanceFiniteMeasurementsSmoke.cs` to replace the negative Length control with a positive finite value and update its two corresponding expected values. Preserve all other measurement assignments, NaN/Infinity rejection, state-retention checks, derived net-concrete assertion and production code unchanged. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 ElementInstance net-concrete finite fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@68b93afd9912d24a5f5890cc4b07c782d202159c`.

The full Core smoke next reaches `ElementInstanceNetConcreteFiniteSmoke.cs`, whose older regression expects over-deduction to produce `-1` and attempts overflow by assigning negative gross/deduction values. Completed domain hardening now permits only finite non-negative stored measurements and floors `NetConcreteM3` at zero, making both negative storage and subtraction overflow unreachable through the public API. Dedicated nonnegative and floor-zero smokes already pin those newer guards. Both owning net-concrete claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/ElementInstanceNetConcreteFiniteSmoke.cs` to expect zero for over-deduction and replace the two unreachable signed-overflow cases with maximum-finite valid controls (`MaxValue - 0` and `MaxValue - MaxValue`). Preserve normal finite arithmetic and production `ElementInstance` code unchanged. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 legacy finite-metric fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@7c43babfd7063b9d84dd0c097f72af4c8a2dd49f`.

The full Core smoke next reaches `EntitySnapshotFiniteMetricsSmoke.cs`, whose older finite control expects a negative area to remain stored despite the completed nonnegative snapshot-metric contract. A source/history audit found two adjacent completed finite-metric fixtures with the same supersession: `OpeningPropertySetFiniteMetricsSmoke.cs` still stores zero width and negative height although physical opening dimensions are now strictly positive, and `RoomWallPropertySetFiniteMetricsSmoke.cs` still stores negative wall thickness although thickness is now strictly positive. Signed sill/base/axis offsets remain valid and must stay unchanged. All newer invariant claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for these exact three fixtures.

Reserve only those three smoke files to use positive finite control values for snapshot area, opening width/height and wall thickness, updating only their corresponding retained-value assertions. Preserve nullable/default values, signed offsets, non-finite rejection, state-retention checks and all production model/domain code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 license signature node-shape fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@fe4337496bc8d91f4173e979cec900aa2dc6c600`.

The full Core smoke next reaches `LicenseSignatureNodeShapeSmoke.AcceptsOrdinaryTextSignature()`, whose older valid control wraps `AA==` in surrounding whitespace. The completed signature Base64 canonicality contract now intentionally rejects both surrounding and embedded whitespace while preserving exact canonical text and the separate non-text-node rejection contract. Both owning licensing claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/LicenseSignatureNodeShapeSmoke.cs` to rename the valid-control method/message to canonical text and use exact `AA==` content. Preserve comment, CDATA and processing-instruction rejection, decoded byte assertion, XML structure and production `LicenseVerifier` unchanged. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 Project Browser dirty-tracking fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@4e49bedf178f560b6fa97a3713a28f1cced3cf8c`.

The full Core smoke next reaches `ProjectBrowserWorkspaceDirtyTrackingSmoke.cs`, whose older regression expects persisted presentation-state Save/Clear to increment semantic `ProjectState.ChangeVersion`, make `ProjectPersistenceStamp.RequiresSave()` true, and overflow at `long.MaxValue`. The completed semantic-version-isolation fix intentionally restored the opposite product contract: workspace presentation metadata mutates without touching semantic revision, so it remains invisible to the semantic persistence stamp and can Save/Clear at maximum semantic version. The old dirty-tracking/revision-atomicity claims and the superseding semantic-version claim are all `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only that smoke file to assert Save/change/Clear mutate only workspace metadata while semantic version/timestamp/stamp remain unchanged; convert the two maximum-version overflow cases into successful presentation-only Save/Clear controls. Preserve changed/no-op return values, metadata presence/removal, exact maximum version and production store/stamp code unchanged. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 active Floor/Zone fixture canonical-repair reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@bb50e290d890ec2f5b147f24445ca59d3b4baba4`.

The full Core smoke next reaches the first two cases in `ProjectFloorZoneMutationIntegritySmoke.cs`, which still expect padded/case-varied `ActiveFloorId` and `ActiveZoneId` aliases to remain stored as no-ops. The completed active-context canonicalization contract now intentionally repairs such aliases to the exact project-owned ID and advances `ChangeVersion` once; only an already exact canonical value is a no-op. The assignment cases in this fixture remain on their separate canonical-equivalent no-op contract. Both owning claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectFloorZoneMutationIntegritySmoke.cs` to rename the two active-alias cases as repairs and assert exact stored IDs plus one version increment. Preserve Floor/Zone assignment no-ops, null-target atomicity, ownership assertions and all production services unchanged. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 interchange UTC defense fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@84df2060da5d1eb4b5cd7e4c180146cd3937cc8b`.

The full Core smoke next reaches `ProjectInterchangeExportSafetySmoke.cs`, whose two exporter-defense cases assign Local/Unspecified values through the public `ProjectState.UpdatedUtc` setter. The completed live-domain UTC invariant now rejects those values before the exporter is invoked, so the fixture no longer reaches the intended `ProjectInterchangeJsonExporter` defense or atomic destination-preservation path. The newer domain invariant claim is `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only that smoke file to corrupt the private timestamp backing field through a test-local reflection helper after constructing an otherwise valid project, allowing the exporter defense-in-depth cases to remain executable without weakening the public setter. Preserve expected `InvalidDataException`, existing-destination preservation, successful replacement and all production domain/export code unchanged. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 QSDB canonical timestamp fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@2f981b82ea778b0ed1351c959f0616d728b05602`.

The full Core smoke next reaches `QsdbProjectSchemaRegressionSmoke.ValidCurrentProjectLoads()`, whose intended-valid current-schema control still uses a second-precision root timestamp. The completed QSDB timestamp-canonicality contract now requires the exact UTC round-trip `O` representation and therefore correctly rejects that stale fixture. A read-only audit found the same completed-contract incompatibility in the intended-valid Schema V2 migration XML in `WorkflowPersistenceSmoke.cs`. The QSDB timestamp-canonicality claim and the older workflow fixture claims are `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for these exact two fixture surfaces.

Reserve only `tests/QS3D.Core.SmokeTests/QsdbProjectSchemaRegressionSmoke.cs` and `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs` to add the seven-digit fractional component to those two intended-valid root UTC timestamp tokens. Preserve every schema/migration/rejection assertion, XML structure and all production persistence code. Re-run the complete Core smoke after this fixture-data-only reconciliation.

## 2026-08-12 revision-capture category fixture compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@c71fb530730d464e7cbbdaa7548f901ce0a5d3c6`.

The full Core smoke cannot compile because the newly completed `RevisionCaptureXmlTextIntegritySmoke.cs` uses the removed `ElementCategory.Wall` enum member in two intended-valid fixture constructors. Current source exposes the specific `ArchitecturalWall` category used by the same semantic wall contract. The owning revision-capture claim is `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/RevisionCaptureXmlTextIntegritySmoke.cs` to replace those two stale enum references with `ElementCategory.ArchitecturalWall`. Preserve the XML-invalid input cases, valid Unicode control, revision capture assertions and all production revision/persistence code. Re-run the complete Core smoke after this fixture-only compile reconciliation.

## 2026-08-12 base door-schedule host-target fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@5818bcec8d0331f2a28ec43de8cb0da976815d4c`.

After the completed Door/opening schedule host-target integrity contract landed, the full Core smoke reaches `DoorOpeningScheduleSmoke.cs`: its intended-valid grouped Door and WallOpening cases assign canonical `HostWallId` values (`wall-a`/`wall-b`) but never add the referenced semantic wall elements. The builder now correctly fails closed before the intended grouping/inheritance assertions. The host-target claim is `COMPLETED`; this fixture is already reserved by the active base-schedule reconciliation expansion, and the current ACTIVE/BLOCKED audit found no overlapping owner.

Extend the existing reservation for `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleSmoke.cs` only to add the minimum matching `ArchitecturalWall` host elements for the intended-valid host IDs. Preserve door/opening rows, dimensions, distinct-host counts, grouping, inheritance/override behavior, invalid numeric case and all production reporting/domain code. Re-run the complete Core smoke after this fixture-data-only reconciliation.

## 2026-08-12 Preview Review composite-key fixture supersession expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@641b04a3c6f68c610c0d9b5464de5f457f988155`.

The full Core smoke reaches `PreviewReviewCompositeRowKeySmoke.cs`, whose older delimiter-collision case constructs U+001F-bearing persisted fields and expects both snapshots to verify. The completed Preview Review XML-text safety contract now correctly rejects U+001F before fingerprint/verification, making that old collision state unreachable. The follow-up section of the `ABORTED (SUPERSEDED)` snapshot-row-key claim explicitly identifies this stale smoke for a separate test-only reconciliation; the original composite-key and XML-text claims are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no owner for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/PreviewReviewCompositeRowKeySmoke.cs` to replace the stale successful-U+001F comparison case with deterministic fail-closed assertions for both former delimiter placements while preserving the valid case-insensitive identity comparison. Do not edit Preview Review source, fingerprinting, validation, persistence or comparison services. Re-run the complete Core smoke after this test-only supersession reconciliation.

### 2026-08-12 Preview Review composite-key expansion release

`RELEASED (CONCURRENT OWNER)`: mandatory pre-commit synchronization exposed the earlier exact-scope claim `docs/agent-work-claims/2026-08-12-1059-chatgpt-web-gpt56sol-preview-review-composite-smoke-reconcile.md` (`8162f77e`) and its implementation `ef84b2d3`. This LOCAL-003 expansion performed no implementation edit and releases `PreviewReviewCompositeRowKeySmoke.cs` to that existing owner; the upstream implementation is consumed only through normal `main` synchronization.

## 2026-08-12 QSDB canonical relation target fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@a0c40b9b7b5503ba8abb39289e6a8505a95760a7`.

The full Core smoke reaches `QsdbPersistedRelationCanonicalReadSmoke.CanonicalRelationsStillLoad()`, whose intended-valid XML stores canonical active Zone/Floor IDs, element Family/Floor/Zone IDs and dependency `E0` while declaring empty Zone/Floor/Family collections and no dependency target element. The completed QSDB active-context and relation referential-integrity contracts now correctly reject that orphan graph after the token-canonicality checks. The original relation-read and active-context claims are `COMPLETED`; the current ACTIVE/BLOCKED audit found no owner for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/QsdbPersistedRelationCanonicalReadSmoke.cs` to give the intended-valid control minimum matching Zone `Z1`, Floor `F1`, Beam Family `FAM-1` and dependency target `E0`, then assert/read the intended `E1` explicitly. Preserve every padded/empty relation rejection, canonical handles/dependencies, schema/timestamps and all production QSDB/domain code. Re-run the complete Core smoke after this fixture-data-only reconciliation.

## 2026-08-12 QSDB timestamp-offset fixture supersession expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@265ab00e83d5ff5d84adbb9e5d22909695a517fc`.

The full Core smoke reaches `QsdbTimestampOffsetSmoke.cs`, whose schema-3 fixture omits the now-required canonical `changeVersion` and whose former valid case expects a `+07:00` timestamp to normalize on load. The completed QSDB persistence-state and timestamp-canonicality contracts require exact UTC round-trip `O` text and intentionally reject semantically equivalent offsets, so the old expectation is superseded. No claim references or reserves this exact fixture in the current ACTIVE/BLOCKED audit.

Reserve only `tests/QS3D.Core.SmokeTests/QsdbTimestampOffsetSmoke.cs` to add canonical `changeVersion=\"0\"`, keep the missing-offset rejection, require the former `+07:00` token to fail closed and add an intended-valid canonical UTC round-trip load control. Preserve schema shape, temporary-file cleanup and all production QSDB/migration/domain code. Re-run the complete Core smoke after this test-only supersession reconciliation.

## 2026-08-12 Room Finish non-negative storage follow-up expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@3b3616b4633de79dd1e73bdda00edde67323c442`.

The full Core smoke returns to the already reserved `RoomFinishGeneratorNumericSafetySmoke.cs`: its two negative-metric generator cases still assign through public `ElementInstance` setters. The completed non-negative measurement contract now rejects those assignments before generator execution, superseding this claim's earlier assumption that finite negative metrics remained constructible. The original generator and measurement claims are `COMPLETED`; no other ACTIVE/BLOCKED claim owns this fixture.

Extend the existing reservation for that smoke file only to inject negative legacy/corrupt `_areaM2` and `_sideAreaM2` state through a test-local private-field helper for the consumed and disabled-output defense cases. Preserve the public NaN/Infinity setter-boundary assertions, valid generation/provenance, output selection and all production `ElementInstance`/Room Finish code. Re-run the complete Core smoke after this fixture-boundary reconciliation.

## 2026-08-12 Locate unknown-root fixture supersession expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@0a33c938f420125dfb8549c8d44e9e2f60384430`.

The full Core smoke reaches `SourceHandleResolverMissingDependencySmoke.PreservesUnknownRootBehavior()`, which still expects an unknown requested root ID to return an empty handle list. The completed missing-requested-root contract now intentionally fails closed with an actionable diagnostic before traversal, so the old expectation is superseded. The missing-dependency, missing-root and structural-freshness claims are `COMPLETED`; the current ACTIVE/BLOCKED audit found no owner for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/SourceHandleResolverMissingDependencySmoke.cs` to require `E-UNKNOWN` to throw `InvalidOperationException` with the missing root in its diagnostic. Preserve valid direct/dependent handle ordering, missing canonical dependency rejection and all production Locate/source-handle code. Re-run the complete Core smoke after this test-only supersession reconciliation.

## 2026-08-12 diagnostic-summary null-issue fixture compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@6b2da3495aca6bced29937ff8683da32c2c1fb88`.

After the workspace semantic-version fixture reconciliation merged, the full Core smoke cannot compile because `ProjectDiagnosticSummaryPreflightSmoke.NullIssueDoesNotCreateDestinationDirectory()` intentionally supplies a null `ModelHealthIssue` to exercise exporter preflight before directory creation, but expresses that malformed entry as a raw `null` inside a non-nullable array. The warnings-as-errors build correctly reports `CS8625` before the runtime defense can be tested. The owning diagnostic-summary preflight claim is `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummaryPreflightSmoke.cs` to mark that one deliberately malformed array entry with the null-forgiving operator. Preserve the runtime null rejection, no-directory-before-preflight assertion, lazy-enumeration failure case, valid export snapshot and all production diagnostic/export/file-I/O code. Re-run the complete Core smoke after this fixture-only nullable compile reconciliation.

## 2026-08-12 Project Interchange canonical timestamp fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@ab29a302a63e75009affced8a0a04fc33f68c18a`.

The full Core smoke next reaches `ProjectInterchangeCanonicalIdentitySmoke.AcceptsExplicitOffsetDeterministically()`, an older positive control that expects `2026-08-10T17:11:12.0000000+07:00` to normalize to UTC. The completed Project Interchange timestamp-canonicality contract now intentionally accepts only the exact UTC round-trip `O` representation emitted by the exporter and rejects non-zero offsets before typed reading. The original canonical-identity fixture predates that contract; its owning claim and the newer timestamp-canonicality claim are completed, and the current ACTIVE/BLOCKED claim plus open-PR audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectInterchangeCanonicalIdentitySmoke.cs` and `scripts/preflight-interchange-canonical-identity.py` to turn the former offset-positive case into an explicit non-zero-offset rejection, add an intended-valid exact UTC `O` read control that asserts the same instant and `DateTimeKind.Utc`, and update the focused gate tokens accordingly. Preserve the existing missing-offset and padded-identity/key rejections, free-text property behavior, fixture graph and all production validator/reader/exporter code. Re-run the complete Core smoke after this test-and-gate reconciliation.

## 2026-08-12 Project Interchange snapshot timestamp-diff fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@488fc84811f75e7ee435dcfb7f6ef3ce6851bc8e`.

The full Core smoke next reaches `ProjectInterchangeSnapshotTimestampDiffSmoke.EquivalentOffsetsDoNotCreateFalseChange()`, whose two controls still feed non-zero-offset timestamp strings through `ProjectInterchangeSnapshotDiff.CompareJson(...)`. That entry point first uses `ProjectInterchangeValidatedSnapshotReader`, and the completed Project Interchange timestamp-canonicality contract now intentionally rejects those non-canonical strings before comparison. The snapshot-diff fixture and focused gate predate the strict validator; their original owner and the newer timestamp-canonicality owner are completed, and the current ACTIVE/BLOCKED claim plus open-PR audit found no reservation for this exact fixture/gate pair.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectInterchangeSnapshotTimestampDiffSmoke.cs` and `scripts/preflight-interchange-snapshot-timestamp-diff.py` to replace the obsolete equivalent-offset control with two independently exported identical canonical UTC snapshots that produce no timestamp change, express the different-instant control as a different exact UTC `O` token, and update the focused gate tokens/message accordingly. Preserve project/element instant comparison, exporter fixture construction and all production validator/reader/diff code. Re-run the complete Core smoke after this test-and-gate reconciliation.

## 2026-08-12 Beam Stirrup intended-valid actual-spacing fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@004cc684707c08ad9f41a179717ab39c38d17d90`.

The full Core smoke next reaches `BeamStirrupMetadataHealthSmoke.AdvancedSnapshotIsAccepted()`, whose shared advanced fixture seeds `GeneratedBeamStirrupActualSpacingM` as `.15`. The completed Beam Stirrup actual-spacing health contract now requires the exact writer-owned `double.ToString("R", CultureInfo.InvariantCulture)` spelling `0.15`, so the intended-valid control correctly receives one `BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL` issue. The original metadata-health, actual-spacing and newer core-metadata canonicality owners are completed; the current ACTIVE/BLOCKED claim plus open-PR audit found no reservation for this exact fixture.

Reserve only `tests/QS3D.Core.SmokeTests/BeamStirrupMetadataHealthSmoke.cs` to change that shared intended-valid actual-spacing seed to `0.15`. Preserve the dedicated explicit noncanonical-spacing rejection in `BeamStirrupActualSpacingHealthSmoke.cs`, legacy compatibility, advanced length/hook/mode mismatch cases and all production planner/builder/health code. Re-run the complete Core smoke after this one-token fixture reconciliation.

## 2026-08-12 Project Interchange validator timestamp fixture/gate reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@4ae556b9e88cff5670b2ce8ea5ec666ba3be793e`.

The full Core smoke next reaches `ProjectInterchangeValidatorCanonicalSmoke.AcceptsExplicitOffset()`, an older positive control that expects a non-zero `+07:00` timestamp offset to validate. The completed Project Interchange timestamp-canonicality contract now intentionally accepts only exact UTC round-trip `O` text and returns `TIMESTAMP_NOT_UTC` for non-zero offsets. Its focused preflight also still requires the removed permissive `DateTimeOffset.TryParse` and `HasExplicitUtcOffset(raw)` implementation tokens. The original validator smoke owner and newer timestamp-canonicality owner are completed, and the current ACTIVE/BLOCKED claim plus open-PR audit found no reservation for this exact fixture/gate reconciliation.

Reserve only `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidatorCanonicalSmoke.cs` and `scripts/preflight-interchange-validator-canonical.py` to turn the former offset-positive case into an explicit `TIMESTAMP_NOT_UTC` rejection, add an intended-valid exact UTC `O` validation control, and update the gate to require the current exact invariant parse, UTC-kind and exact round-trip reproduction tokens. Preserve missing-offset, padded identity/key/unit rejection coverage, typed-reader unit rejection and all production validator/reader/exporter code. Re-run the complete Core smoke after this test-and-gate reconciliation.

## 2026-08-12 Curtain Panel core intended-valid metadata fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@9e6a5cc371b93a8df6a683775d7b9b59359421f0`.

The full Core smoke next reaches `CurtainPanelCoreSmoke.OwnershipHealthReleaseAndStaleAreIntegrated()`, whose shared intended-valid panel fixture predates the now-required `GeneratedCurtainPanelAreaM2` snapshot and therefore receives `CURTAIN_PANEL_AREA_INVALID`. The same file's completed-empty control must carry canonical zero area, and its later path-mode control now also needs the writer-default canonical `GeneratedCurtainPanelPathSagittaM=0.002`. The area-health and path-sagitta claims are completed; their dedicated missing/noncanonical smokes remain authoritative. The current ACTIVE positive-float canonicality lane reserves only the production `Positive(...)` helper plus a new focused smoke and does not overlap this exact fixture. No other ACTIVE/BLOCKED claim or open PR reserves `CurtainPanelCoreSmoke.cs`.

Reserve only `tests/QS3D.Core.SmokeTests/CurtainPanelCoreSmoke.cs` to seed canonical area `2` on the shared two-piece healthy panel, override canonical area `0` on the completed-empty build, and seed canonical path sagitta `0.002` when that control switches to path mode. Preserve dedicated area/sagitta missing and alias rejection smokes, empty-handle compatibility, ownership/live/stale/release assertions, fingerprint inputs and all production Curtain planner/writer/health code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-12 template-save size fixture category compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@b9833b81f1c7caebeaa4a75a574c434842547368`.

The strict Core smoke cannot compile because the newly completed `TemplateSaveSizePreflightSmoke.cs` constructs its oversized intended-valid family with the removed generic `ElementCategory.Wall` enum member. The current domain API exposes `ArchitecturalWall` as the canonical ordinary wall category, and the fixture only needs a valid family category so it can exercise the independent pre-IO serialized-size rejection. The template-save size claim is `COMPLETED`; the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact compile reconciliation.

Reserve only `tests/QS3D.Core.SmokeTests/TemplateSaveSizePreflightSmoke.cs` to replace the obsolete enum member with `ElementCategory.ArchitecturalWall`. Preserve the oversized UTF-8 payload, exact exception, no-directory/no-file assertions, cleanup behavior and all production template/domain code. Re-run the focused template smoke and complete Core smoke after this one-token fixture reconciliation.

## 2026-08-12 Shape Rebar fixture category compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@1df800d9e3c997b5ddd4a94b6f960248815664ae`.

After the template fixture compiles, the strict Core smoke exposes two simultaneous `CS0117` errors: `GeneratedRebarCountDiameterCanonicalitySmoke.cs` line 103 and `ShapeRebarModeHealthSmoke.cs` line 66 still construct generic Shape Rebar health fixtures with the removed `ElementCategory.Wall` member. Both fixtures only need a valid ordinary wall category to exercise their independent generated Shape Rebar metadata contracts, and current source exposes `ArchitecturalWall` for that intent. Both owning Shape Rebar health claims are `COMPLETED`; the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for these exact fixture lines.

Reserve only `tests/QS3D.Core.SmokeTests/GeneratedRebarCountDiameterCanonicalitySmoke.cs` line 103 and `tests/QS3D.Core.SmokeTests/ShapeRebarModeHealthSmoke.cs` line 66 to replace the obsolete enum member with `ElementCategory.ArchitecturalWall`. Preserve every handle/count/diameter/mode token, health issue expectation, no-handle control and all production generated-rebar code. Re-run both focused health fixtures, the complete Core smoke and focused/general gates after this two-token test-only reconciliation.

## 2026-08-12 Physical Opening structural-freshness message fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@a4418174690f2fd74e169695a3cb61683ca2858c`.

The full Core smoke next reaches `PhysicalOpeningGlobalElementIntegritySmoke.RejectsHostRemovedDuringLazyTargetEnumeration()`. Its operation still correctly fails closed without mutating revision, dirty state, timestamps or `HostWallId`, but the fixture expects the older host-specific ownership message. The completed Physical Opening target structural-freshness contract now detects the deliberate list removal at the stronger ordered-project-snapshot boundary and returns the canonical structural-drift message before the older host check. Both owning Physical Opening claims are `COMPLETED`; the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact expected-message line.

Reserve only `tests/QS3D.Core.SmokeTests/PhysicalOpeningGlobalElementIntegritySmoke.cs` line 54 to replace the stale expected text with `Project element structure changed while physical opening target ids were being enumerated; recompute the target set against the current project state.` Preserve the deliberate host removal, exception type, project `ChangeVersion`, host/opening dirty and timestamp state, `HostWallId`, duplicate-integrity and valid-resolution assertions, and all production Physical Opening code. Re-run the focused smoke, complete Core smoke and focused/general gates after this one-string test-only reconciliation.

## 2026-08-12 Semantic Tag rotation fixture category compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@4d3cd692ffbe3e44dd1bab0e615b749ba87ae608`, after placement-canonicality PR `#889` merged.

The strict Core smoke cannot compile because the newly completed `SemanticTagRotationHealthSmoke.cs` line 74 constructs its generic Semantic Tag health fixture with the removed `ElementCategory.Wall` member. The fixture only needs a valid ordinary wall category to exercise rotation metadata, and current source exposes `ArchitecturalWall` for that intent. The rotation-health owner is `COMPLETED`; placement canonicality touched the shared production health service plus a new dedicated smoke but not this rotation fixture, and the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact line.

Reserve only `tests/QS3D.Core.SmokeTests/SemanticTagRotationHealthSmoke.cs` line 74 to replace the obsolete enum member with `ElementCategory.ArchitecturalWall`. Preserve every rotation, handle, template, text, owner, placement and health-issue token and all production Semantic Tag code. Re-run the focused rotation smoke, placement/rotation gates and complete Core smoke; if the merged placement contract exposes a separate runtime expectation drift, audit and claim it independently rather than changing it under this reservation.

## 2026-08-12 Semantic Tag placement fixture category compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@40502704b402b1aa55300f7f187b4fabd355eb40`, after ownership-version canonicality PR `#895` merged.

After the rotation fixture compiles, the strict Core smoke next exposes `CS0117` in `SemanticTagPlacementCanonicalitySmoke.cs` line 99 because its generic Semantic Tag placement fixture uses the removed `ElementCategory.Wall` member. The fixture only needs a valid ordinary wall category to exercise placement snapshot canonicality, and current source exposes `ArchitecturalWall` for that intent. The placement and ownership-version claims are both `COMPLETED`; ownership-version changed the shared health service plus its own dedicated smoke but did not touch this placement fixture, and the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact line.

Reserve only `tests/QS3D.Core.SmokeTests/SemanticTagPlacementCanonicalitySmoke.cs` line 99 to replace the obsolete enum member with `ElementCategory.ArchitecturalWall`. Preserve every placement scope/coordinate/text-height token, precedence and health-issue assertion, all ownership-version and rotation coverage, and all production Semantic Tag code. Re-run the focused placement, ownership-version and rotation smokes, complete Core smoke and focused/general gates after this one-token test-only reconciliation.

## 2026-08-12 Semantic Tag ownership-version fixture category compile reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@481434a92177f15307b7fb68eb174dcab810f8ef`.

After the placement fixture compiles, the strict Core smoke next exposes `CS0117` in `SemanticTagOwnershipVersionCanonicalitySmoke.cs` line 65 because its generic Semantic Tag ownership-version fixture uses the removed `ElementCategory.Wall` member. The fixture only needs a valid ordinary wall category to exercise the exact writer-owned ownership-version token, and current source exposes `ArchitecturalWall` for that intent. The ownership-version claim is `COMPLETED`; the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact line.

Reserve only `tests/QS3D.Core.SmokeTests/SemanticTagOwnershipVersionCanonicalitySmoke.cs` line 65 to replace the obsolete enum member with `ElementCategory.ArchitecturalWall`. Preserve every ownership-version, owner, handle, placement, rotation and health-issue token and assertion, and all production Semantic Tag code. Re-run the focused ownership-version, placement and rotation smokes, complete Core smoke and focused/general gates after this one-token test-only reconciliation.

## 2026-08-12 revision legacy dependency-section fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@e79ca1a613306efd39ae5c926f510744f6f4491b`.

The full Core smoke next reaches `RevisionDependencyFreshnessSmoke.DependenciesRoundTripAndLegacyLoads()`, whose intended-valid legacy XML omits the `<dependencies>` container and expects the loader to normalize that malformed shape to an empty set. The completed Revision required-element-sections contract now intentionally requires exactly one `<properties>`, `<quantities>`, `<sourceHandles>` and `<dependencies>` container because the serializer always emits them; `preflight-revision-snapshot-schema.py` explicitly locks all four exact-one calls and forbids optional validation. The dependency-freshness and schema owners are `COMPLETED`; the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact legacy fixture line.

Extend the existing reservation for `tests/QS3D.Core.SmokeTests/RevisionDependencyFreshnessSmoke.cs` only to add an empty `<dependencies/>` container to that intended-valid legacy element. Preserve the empty dependency-set assertion, valid two-dependency round trip, dependency-only comparison, malformed dependency XML cases, cleanup and all production Revision validator/store/compare code. The focused schema gate already expresses the current contract and must remain unchanged. Re-run the focused revision smoke, schema gate and complete Core smoke after this one-token fixture-shape reconciliation.

## 2026-08-12 revision legacy timestamp fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@994f2527e1e1b2990353e98fd1e0bef8de686770`.

After the intended-valid legacy element includes its required dependency section, the full Core smoke reaches the same `RevisionDependencyFreshnessSmoke.DependenciesRoundTripAndLegacyLoads()` XML at line 83 and rejects its short-form `createdUtc='2026-08-11T00:00:00Z'`. The completed Revision timestamp-canonicality contract requires the exact invariant UTC `O` representation emitted by the serializer and explicitly rejects short-form UTC text. The timestamp owner is `DONE`, and the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact intended-valid token.

Extend the existing reservation for `tests/QS3D.Core.SmokeTests/RevisionDependencyFreshnessSmoke.cs` line 83 only to replace that intended-valid timestamp with `2026-08-11T00:00:00.0000000Z`. Preserve the empty dependency-set assertion, required `<dependencies/>`, valid two-dependency round trip, dependency-only comparison, malformed dependency XML cases, cleanup and all production Revision timestamp/schema/store/compare code. Do not change the separate malformed-input timestamp at line 124 under this reservation. Re-run the focused revision smoke and complete Core smoke after this one-token fixture reconciliation.

## 2026-08-12 Revision Snapshot identity fixture canonical-shape reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@705682b5833af9a631b97a56de6656d21e483ab2`.

The full Core smoke next reaches `RevisionSnapshotIdentityCanonicalitySmoke.CanonicalAndLegacyOptionalIdentityLoads()`. Its shared `Document(...)` helper and intended-valid legacy-optional XML still use short-form `createdUtc='2026-08-11T00:00:00Z'`, while the completed Revision timestamp contract requires the exact invariant seven-digit UTC `O` representation emitted by the serializer. The legacy-optional element also omits the now-required empty `<dependencies/>` section. The original identity fixture owners and the timestamp/schema owners are completed; the current ACTIVE/BLOCKED claim and open-PR audit found no reservation for this exact fixture. `preflight-revision-snapshot-schema.py` already locks the canonical production shape and does not token-lock this smoke fixture.

Reserve only `tests/QS3D.Core.SmokeTests/RevisionSnapshotIdentityCanonicalitySmoke.cs` to change the shared helper and intended-valid legacy-optional timestamp tokens to `2026-08-11T00:00:00.0000000Z`, and add `<dependencies/>` to the intended-valid legacy-optional element. Preserve the missing optional identity assertions, padded revision/element/optional-identity rejections, category-casing/padding rejections, cleanup and all production Revision validator/store/identity behavior. Keep the focused schema gate unchanged. Re-run the focused identity smoke, Revision schema gate and complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 authoritative layer-mapping fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@f2c2f6ce02c64af40d19ae16f7e33b6590ed695e`.

The complete Core smoke on the clean current baseline reaches `WorkflowPersistenceSmoke.ProjectLayerMappingOverridesFallback()` and reports `Expected Beam, got Door`. Commit `bc76dfc12c7e79a63940dea5e69a3ec46a968ce4` deliberately restored authoritative project layer mappings by removing entity-type fallback for an exact mapping, while this legacy assertion still expects the fallback Beam category even though the test configures the exact `A-BEAM -> Door` project mapping. The active layer-mapping canonicality reservation owns production validation/canonicality surfaces but does not reserve this legacy workflow fixture; the current ACTIVE/BLOCKED audit found no other owner for this exact assertion.

Extend the existing reservation for `tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs` only to expect the explicitly mapped `Door` category in this authoritative-mapping control. Preserve the configured mapping, fallback evidence, confidence/review and batch auto-accept assertions, all other workflow/persistence fixtures, and all production recognition/template code. Re-run the complete Core smoke after this one-line fixture reconciliation.

## 2026-08-13 Auto Room stale-finish Family fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@a6d2a8c2428ab2428bcd33b31d5d139144b58c7b`.

After the authoritative layer-mapping fixture was reconciled concurrently on `main`, the complete Core smoke reaches `AutoRoomLifecycleSmoke.StaleRoomsAndDependentsAreExcludedFromBq()`. Its intended-valid `STALE-FINISH-PROPERTY` element has category `CeilingFinish` but references the shared `finish` Family whose category is `FloorFinish`. The completed reporting Family-category integrity contract now correctly rejects that malformed project before quantity exclusion is evaluated. Existing Auto Room fixture owners and the reporting integrity owner are `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact stale-exclusion fixture.

Reserve only `tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs` to give the property-only ceiling finish a project-owned `CeilingFinish` Family while preserving its Room provenance, stale/excluded assertions, the independent FloorFinish dependency case, nested dependency exclusion, active Room BQ row, and all production Auto Room/reporting code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 Room Finish boundary-handle fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@a1c652ed1880eca151d2f9312c4fc1cd70b2405e`.

After the Auto Room Family fixture is valid, the complete Core smoke reaches `RoomFinishHealthSmoke.PropertyOnlyRoomProvenanceResolvesBoundaryHandles()`. The test writes `BoundarySourceHandles` as `A1; b2;A1` and expects Locate to trim, case-normalize and deduplicate it. The completed Auto Room boundary-handle canonicality contract now correctly rejects that representation because the production writer emits already-normalized uppercase, distinct, deterministically ordered tokens and Locate must fail closed on persisted drift. The boundary-handle owner and prior Room Finish Health owners are `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact fixture method.

Reserve only `tests/QS3D.Core.SmokeTests/RoomFinishHealthSmoke.cs` to use the canonical `A1;B2` writer representation in this intended-valid property-only provenance control and assert the same two logical handles. Preserve Room provenance resolution, stale/scope Health cases, every other fixture and all production Auto Room/Locate code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 Semantic Tag noncanonical-owner fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@47978b60c2e2d0c46373273174a5985ecb3e0927`.

After the prior fixture reconciliations, the complete Core smoke reaches `SemanticTagRendererSmoke.NonCanonicalOwnerIdsFailClosed()`. The completed semantic-owner canonicality contract correctly validates raw owner IDs inside `SemanticTagRenderContext.Add(...)`, but the regression constructs `ProjectFamily`, `FloorDefinition` and `ZoneDefinition` directly with padded IDs. Those public constructors canonicalize IDs immediately, so the test never supplies malformed in-memory owner state and cannot exercise the guard it claims to pin. The semantic-owner source/regression owner is `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact fixture correction.

Reserve only `tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs` to build canonical owner instances and then use a bounded fixture-only helper to inject the three padded raw `Id` backing values before rendering. Preserve the public constructor normalization contract, Family/Floor/Zone fail-closed assertions, reference/ambiguity/element-identity coverage and all production documentation/domain code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 Semantic Tag healthy-rotation fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@e2f84bf3a816aa0bd59f70e22cf069c830b5613c`.

After the Semantic Tag owner fixture reaches the intended production guard, the complete Core smoke reaches `GeneratedSemanticTagHealthSmoke.HealthyTagPasses()` and reports `SEMANTIC_TAG_ROTATION_INVALID`. The completed rotation-health contract requires every generated tag with handles to persist finite canonical `GeneratedSemanticTagRotationRad`; the shared healthy fixture predates that field and therefore no longer describes a complete writer-owned tag snapshot. The rotation-health owner is `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this exact shared fixture.

Reserve only `tests/QS3D.Core.SmokeTests/GeneratedSemanticTagHealthSmoke.cs` to add canonical zero rotation (`0`) to the intended-healthy shared tag fixture. Preserve missing/invalid/noncanonical rotation diagnostics, semantic text-stale detection, owner/position corruption cases, optional no-tag behavior and all production tag Health/runtime code. Re-run the complete Core smoke after this one-line fixture reconciliation.

## 2026-08-13 auto-sheet worst-case prefix fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@7ee0ec1633cc66e53c847082bfe9105f5cf9e534`.

After the Semantic Tag fixtures are complete, the full Core smoke reaches `SemanticSheetAutoLayoutSmoke.SheetNumberPrefixReservesGeneratedSuffix()`. This legacy method still treats a 62-character prefix as valid and 63 as invalid, while the completed worst-case ordinal contract reserves five characters for supported sheet ordinal `10000`; production therefore correctly caps the prefix at 59 characters before enumeration. The dedicated worst-case smoke already pins `59 + "10000"` at the 64-character downstream limit. All auto-sheet prefix owners are `COMPLETED`, and the current ACTIVE/BLOCKED audit found no reservation for this legacy fixture method.

Reserve only `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs` to use a 59-character accepted prefix, expect its first `01` number to be 61 characters, and reject a 60-character prefix. Preserve deterministic packing/pagination, the separate worst-case ordinal coverage, downstream 64-character number contract, all other auto-layout assertions and all production documentation code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 Domain mutation Auto Room reference fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@b70f0c679daa5099c0ffcfb964e549b4dec81e8f`.

After the Semantic Tag and auto-sheet fixture reconciliations, the full Core smoke reaches `DomainMutationAtomicitySmoke.AutoRoomStaleOverflowDoesNotCommit()`. Its intended-valid source room references `FloorId=L1` and `ZoneId=Z1`, but the fixture never adds those project-owned Floor and Zone records before `AtVersion(...)` round-trips the project through QSDB. The completed global reference-integrity contract therefore correctly rejects the orphan references before the test can inject `long.MaxValue` and exercise Auto Room overflow atomicity. The original domain-mutation and persistence-integrity owners are `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact fixture correction.

Reserve only `tests/QS3D.Core.SmokeTests/DomainMutationAtomicitySmoke.cs` to add project-owned `FloorDefinition("L1", ...)` and `ZoneDefinition("Z1", ...)` records to that intended-valid source fixture. Preserve the deliberate `long.MaxValue` version injection, overflow exception, no-commit/no-mutation assertions, every other atomicity scenario and all production Auto Room/domain/persistence code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 QSDB non-UTC project timestamp fixture reconciliation expansion

Baseline audited after a clean fetch and fast-forward: `origin/main@2d3736ea53429890b6784d8309cf82a41b1ec051`.

The full Core smoke next reaches `QsdbCanonicalPersistenceSmoke.NonUtcTimestampFailsBeforePersistence()`. Its project-timestamp control directly assigns an `Unspecified` value and then expects QSDB save to reject it, but the completed `ProjectState.UpdatedUtc` UTC-invariant contract now rejects that assignment atomically at the earlier domain boundary. The same method's malformed AuditEvent remains intentionally mutable and must still reach the QSDB preflight. The UpdatedUtc invariant and QSDB canonical persistence owners are `COMPLETED`; the current ACTIVE/BLOCKED audit found no reservation for this exact fixture correction.

Reserve only `tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs` to assert that the non-UTC project assignment throws `ArgumentException` without changing the prior UTC timestamp, while preserving the Local AuditEvent persistence rejection, no-output-file guarantees, every other canonical persistence case and all production domain/persistence code. Re-run the complete Core smoke after this fixture-only reconciliation.

## 2026-08-13 coherent Level source-candidate integration

The reserved Level chain was reapplied onto moving `main` through `9c0a25d6` after re-reading every concurrently changed surface. The source now uses `CadElementVerticalPlacement` as the branch-lazy adapter for qualified wall/structural hosts, hosted openings, Curtain LINE/path output, longitudinal/transverse/mesh/shape reinforcement, live fingerprints and generated vertical snapshots. Semantic quantity regeneration, Level Health, opening-host invalidation and guarded Floor/Level Bottom/Top/Clear UI share the same Core placement contract. The concurrently merged Floor opposite-reference structural-freshness guards remain intact.

Curtain P02-P08 automation probes merged while this batch was in progress and still compile against the first-wave resolver surface. To avoid editing those separately reserved probe files, the old source file is removed and a narrow compatibility facade lives beside the canonical adapter. It forwards explicit legacy probe placement, hosted-opening resolution and `HasConfiguredLevel(...)` to the new adapter; production builders do not retain independent Level arithmetic. The P02/P05 direct `ReadLineOpenings(...)` calls are preserved through a forwarding overload in `CurtainWallPanelBuilderSupport`.

Current source validation before the candidate commit: complete Core Release smoke `ALL PASS`; focused Level reference/native-host/opening/Curtain/rebar/runtime-probe gates PASS; Beam Rebar plus Curtain P07/P08 compatibility gates PASS; installed BricsCAD V25 `Release|x64` build succeeds with zero warnings/errors. The current 719-gate aggregate passes 718 gates; its only failure is the independently owned release-version mismatch (`QS3D.BricsCAD.V25` preview.4 versus `QS3D.Core` preview.3) on current `main`. This LOCAL_ONLY claim does not change release versioning, consume CI failures as a backlog or dispatch CI. Runtime evidence remains `PENDING_LOCAL` until the coherent source batch is committed/pushed and the clean exact-SHA probe is executed.

The later local-code capability lock does not broaden this reservation: this coherent adapter/probe batch is the already-assigned `LOCAL-003 / PENDING_LOCAL` integration that was authored and compiled against the installed proprietary BricsCAD V25 API/runtime and is required to make the licensed native probe executable. It does not reserve or fix any unrelated remote-safe bug, general audit item or CI failure. Any new defect that does not itself require local-only capability will be handed to a non-local agent instead of being absorbed here.

## 2026-08-13 exact-SHA runner prelaunch correction

Clean pushed candidate `45cf28c7af9be9df4949c05a975512ec253a747b` rebuilt against installed BricsCAD V25 at zero warnings/errors. Both plugin and Core ProductVersion values ended in that exact SHA, the focused gates passed and Core smoke reported `ALL PASS`. The first guarded runner attempt did not enter BricsCAD: its `git rev-parse` output was the correct single 40-character SHA, but piping it through `Select-Object -First 1` caused this Windows PowerShell host to leave `$LASTEXITCODE=-1`, so the prelaunch guard rejected two visibly identical SHAs.

The already-reserved local runner now captures the native Git output and exit code before any PowerShell pipeline processing, requires exactly one hexadecimal 40-character result, separately captures the worktree-status exit code and adds a static guard against reintroducing the early-closing pipeline. This is a LOCAL_ONLY harness correction observed on the installed Windows runtime path; it does not change Level product logic or convert any unrelated validation failure into local coding scope. A new clean pushed SHA/build and fresh artifact root are required for the retry. Runtime remains `PENDING_LOCAL`.

## 2026-08-13 blocked handoff

Status is now `BLOCKED` after the same two external conditions repeated across three consecutive goal turns. Synchronized `main@e2ef91fccd3019ccc6dfeaa4dc14b905bec859ad` differs from the pushed Level candidate only through later concurrent work, but its strict installed-reference V25 build stops before Level/native compilation on 15 nullable compiler errors in the independently merged remote-safe `src/QS3D.Core/Measurement/MeasurementTrace.cs`. Under the local-worker capability lock this agent must report that observation and must not claim or repair the general Core defect. No matching MTR/measurement correction branch was visible remotely at the final audit.

The user-owned BricsCAD V25 process remains open from Explorer with `Drawing1`, so the isolated runner must not launch or close that session on the user's behalf. Resume this claim only after a non-local agent lands the MTR compiler correction and the user saves/closes every BricsCAD session. On resume, set the claim/inbox back to `IN_PROGRESS`, fetch current `main`, rebuild plugin/Core with zero warnings/errors, verify exact ProductVersion SHA, create a fresh disposable artifact root and rerun the full focused probe. No GitHub Actions were dispatched.

## 2026-08-13 resumed after prerequisite clearance

The claim resumes from synchronized `origin/main@b3495e36dace537cd8de823b5138c0899b4a68db`. Non-local commit `bf671a90` corrected the independently owned `MeasurementTrace` nullable contract, and its claim completion is present on this baseline. The user-owned BricsCAD `Drawing1` process has been saved/closed without agent termination. The concurrent Curtain P09 merge and quantity-rule claim were read before this status change and remain outside this Level scope.

This status-only reactivation precedes all renewed qualification. After the reactivation commit is visible on `origin/main`, rebuild the current exact SHA against installed BricsCAD V25 with zero warnings/errors, verify plugin/Core `ProductVersion`, run the focused deterministic gates, create a fresh disposable `*.level-z-probe-copy.dwg`, and execute the guarded Level-Z runtime probe. Do not absorb any new remote-safe failure or dispatch GitHub Actions.

## 2026-08-13 first resumed runtime observation and diagnostic hardening

Exact source SHA `1ebfd929e05db6c441b0a62fbc8632fcfcfe1f69` built against installed BricsCAD V25 with zero warnings/errors, both plugin/Core `ProductVersion` values ended in that SHA, and the focused Level/Curtain/Rebar gates passed. The guarded probe entered licensed BricsCAD V25 and preserved the disposable drawing hash, emitted no sidecar, restored its three process environment variables and left no BricsCAD process, but returned sanitized code `LEVEL_Z_RUNTIME_CURTAIN_FAILED` after proving legacy wall range `1.2 m .. 3.7 m`. That code is too coarse to distinguish frame build, panel build, native range or semantic mode validation. The failed run also showed that the generated private `.scr` survived the runner `finally`; it was removed from the exact verified artifact root while the sanitized marker was retained.

Within the already reserved Level runtime-probe surface, split the Curtain phase into four bounded, identity-free error codes and make runner cleanup remove its exact generated script on both PASS and FAIL. Keep exception text, paths, handles and semantic identities out of the marker. The static gate must lock the granular codes and exact-script cleanup before a clean pushed exact-SHA rerun.

The next two clean exact-SHA launches never reached the command: the runner-owned BricsCAD process remained responsive but created no main window, generated no marker and timed out with drawing/sidecar/process/environment/script cleanup intact. Read-only inspection of the dedicated `QS3D-V25-TEST` profile found one `Drawing Recovery/Opened` record pointing exactly to the first disposable Level-Z copy. The failed probe rethrew after writing its marker, and the runner force-stopped the host before BricsCAD could clear the recovery record; subsequent hidden starts waited before script execution.

Harden this already-reserved harness boundary so both PASS and FAIL return normally to the automation script after the sanitized marker, the script issues localized-safe `QUIT` with No-save, and the runner waits for and requires graceful exit before accepting PASS. Keep force termination only as a cleanup fallback. Remove only the exact stale recovery value pair that resolves under this repository's `artifacts/local-v25-level-z` root and has the guarded disposable suffix; preserve every other profile/recovery value. A fresh pushed exact-SHA run is required.

## 2026-08-13 native host startup blocker

The graceful no-save/diagnostic harness landed on `main` at exact SHA `38c22f168e530908a0eadebb5e740b16b2388939` after static validation and a strict installed-reference V25 build with zero warnings/errors. Before that correction could reach the plugin, repeated test-owned BricsCAD V25 launches stopped in native startup: the exact launched PID remained responsive at about one second of CPU, created no main application window, loaded neither `QS3D.BricsCAD.V25.dll` nor `QS3D.Core.dll`, wrote no marker and then accepted exact-PID timeout cleanup. Every disposable DWG retained SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; no sidecar, backup, script, environment variable or BricsCAD process remained.

The startup failure reproduced with no DWG/plugin/script, with clones of both `QS3D-V25-TEST` and `Default`, with an officially clean new profile plus `/SAFEMODE /L`, and with explicit `/pr ultimate`. This rules out the Level runner, disposable drawing, test profile and third-party autoload as the immediate startup boundary. All temporary profiles were removed; original `Default` and `QS3D-V25-TEST` remain. The item is reserved but `BLOCKED` until Windows session/BricsCAD license-native startup is refreshed (normally sign out/restart Windows or operator license repair) and a test-owned BricsCAD launch can create its application window again. After recovery, reactivate/push the claim first, sync/rebuild the newest exact SHA, then rerun the granular Level probe. No GitHub Actions ran, and no user-owned BricsCAD process/profile/drawing was terminated or modified.

## 2026-08-13 resumed after Windows restart

The Windows host restarted and a normal operator-owned BricsCAD V25.2.10 x64 session now has a responsive main application window, so the earlier native-startup machine blocker is cleared. This status-only reactivation is based on synchronized `origin/main@561523e48ac1eaad3e3b5722cf74b3f7446b58dc` and must be visible on `origin/main` before renewed qualification begins.

The operator-owned session currently has `MB MONG.dwg` open and remains strictly out of scope. The exact-SHA runner will not start, close or modify that process/drawing; it will wait until the operator saves and closes every BricsCAD session. After that precondition is satisfied, fetch current `main` again, rebuild the newest exact SHA against the installed V25 references, verify matching plugin/Core `ProductVersion`, run the focused deterministic gates and execute the granular Level probe from a fresh disposable artifact root. No GitHub Actions are authorized.

## 2026-08-13 repeated blocker handoff

The reactivated lane synchronized through `origin/main@3752d589634d5a909fb58691150f6fac2905c225`. All nine focused Level/static gates passed, and `QS3D.Core` Release built with zero warnings and zero errors under the local .NET 8.0.423 SDK. The required full Core smoke project then failed to compile at `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs:98` with `CS8602`. This is a CAD-independent, remote-safe test nullable-flow defect; it was handed to non-local issue `#990` and this local worker did not edit the test or production MeasurementTrace source.

Across three consecutive goal turns, issue `#990` remained open without an assignee/fix and the operator-owned responsive BricsCAD session continued to hold `MB MONG.dwg`. The runner correctly refuses any pre-existing BricsCAD process, and the local worker must neither close that process nor bypass the mandatory full-smoke gate. The claim is therefore `BLOCKED` while preserving its reservation. Resume only after a non-local fix makes the full Core smoke pass and the operator saves/closes every BricsCAD session; then reactivate/push the claim, fetch the newest `main`, rebuild and rerun all exact-SHA gates plus the fresh disposable Level probe. No GitHub Actions were dispatched.

## 2026-08-14 resumed after both prerequisites cleared

The lane resumes from synchronized `origin/main@93a5547224a5248ae741ccd8dd4368bac27b6b00`. Issue `#990` is `CLOSED / COMPLETED`; current `MeasurementTraceContractSmoke` uses the corrected null-safe equality assertion. A fresh process audit found no running BricsCAD process, so the earlier operator-session exclusion is also clear without the agent closing or modifying the user's drawing.

This status-only reactivation must be visible on current `origin/main` before renewed qualification. After publication, fetch again, re-audit concurrent claims, build Core and the V25 adapter from the newest exact SHA, require the complete smoke and focused Level/static gates to pass, verify matching plugin/Core `ProductVersion`, create a new disposable `*.level-z-probe-copy.dwg` and fresh artifact directory, then execute `scripts/test-bricscad-v25-level-z.ps1`. Any new CAD-independent defect is handed to a non-local owner; no GitHub Actions are authorized.

## 2026-08-14 current-main smoke blocker handoff

Qualification on clean exact SHA `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` passed the Core Release build with `0 warnings / 0 errors` and all nine focused Level/static gates. The installed-reference BricsCAD V25 `Release|x64` build also passed with `0 warnings / 0 errors`; plugin SHA-256 `CCC99A7BF822BB1FF41C60CBC074C444178C8E1C144ABD68967C8D5549088968` and Core SHA-256 `722F6B3518C3A7545C260170E0011FD9252CA83487EF49D3683E4B4485C2D4C6` both report `ProductVersion 0.1.0-preview.5+2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd`.

The mandatory full Core smoke then failed in the CAD-independent `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity` assertion: numeric aliases `A` / `00a` produced zero `DUPLICATE_SOURCE_HANDLE` issues instead of one. Current `ModelHealthService` trims stored SourceHandles instead of applying the shared numeric handle normalization expected by the registered smoke. The sanitized handoff is GitHub issue `#1092`; this local worker did not edit Core or the test fixture. The native Level probe was not run because full smoke is a prerequisite. This claim remains reserved but `BLOCKED` until a non-local fix makes the complete smoke pass on current `main`; afterward reactivate/push before rebuilding and running a fresh disposable exact-SHA probe. No GitHub Actions were dispatched.

## 2026-08-14 source blocker cleared; operator session active

Issue `#1092` is `CLOSED / COMPLETED`; fixes `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4` and `337e97d32b6642b3a3d013596ffa1545df168999` have fresh deterministic Core smoke evidence on exact SHA `337e97d32b6642b3a3d013596ffa1545df168999`. The local checkout synchronized through `origin/main@e2171f4cf63d5502fb31e8a30644d6c9739100e3` and the overlapping Core-smoke claim is completed.

The remaining immediate blocker is environmental: a responsive operator-owned BricsCAD V25 session is open on `MB MONG.dwg`. This worker did not start, close, or modify that session. Keep the claim `BLOCKED`; after the operator saves and closes every BricsCAD process, fetch newest `main`, reactivate/push this claim and the inbox status, then rerun full smoke, focused gates, exact installed-reference build and a fresh disposable Level probe. No GitHub Actions are authorized.

## 2026-08-14 Level runner launcher-to-host `NO_RESULT` harness expansion

Exact synchronized candidate `f38cc3464a11c62df31d50c186012a654b192e1f` passed the full Core smoke, all nine focused Level/static gates and the installed-reference BricsCAD V25 `Release|x64` build with zero warnings/errors. Matching plugin/Core `ProductVersion` values ended in that exact SHA. A clean licensed Level probe then reached the guarded 240-second timeout without producing its sanitized marker. No BricsCAD process survived, the private script and process environment were removed/restored, no sidecar/backup/lock file remained, and the disposable DWG returned byte-for-byte to repository SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. This is `NO_RESULT`, not a product PASS or FAIL.

The already reserved Level qualification harness is expanded narrowly to diagnose and correct launcher-to-host process ownership in `scripts/test-bricscad-v25-level-z.ps1`, with a focused Level runner static guard only if needed. `scripts/bricscad-runner-window-interop.ps1` may be read and consumed as the existing shared helper, but is explicitly read-only in this expansion. No Curtain P10/P11 runner, LOCAL-004 runner, shared helper, product/Core source, GitHub Actions or unrelated qualification lane is reserved. If a shared-helper edit proves necessary, stop and publish a separate collision-checked claim amendment before that edit.

Acceptance requires the Level runner to bind only to a test-owned BricsCAD host created after a zero-process preflight, distinguish launcher exit/handoff from host exit, execute the existing private script, emit the existing sanitized exact-SHA marker, and clean only test-owned processes/files/state on PASS, FAIL or timeout. Do not attach to, close or modify any pre-existing/operator-owned BricsCAD process or drawing. A changed harness must pass its deterministic guard and then a fresh disposable exact-current-SHA licensed run; an unchanged blind retry is not sufficient.

## 2026-08-14 native startup blocker confirmed below the Level harness

The `NO_RESULT` is not a launcher-to-host tracking defect. Four zero-process diagnostic launches covered hidden and visible `/P QS3D-V25-TEST`, the installed BricsCAD working directory, and the official no-argument BricsCAD shortcut. Every launch created exactly one responsive `bricscad.exe`, no child/host handoff and no main window for 30 seconds; CPU settled near one second. A separate 12-second snapshot of the official shortcut launch found 256 loaded modules and 22 threads but only invisible Qt screen-observer and IME top-level windows. FlexNet Licensing Service was running, no recent BricsCAD WER/crash record existed, and each exact test-owned PID was removed after the zero-process preflight with zero BricsCAD processes remaining.

This is `BLOCKED_LOCAL_ENVIRONMENT / NO_RESULT`, not a Level product PASS or FAIL. No runner/helper/product change is justified by the evidence, so this worker did not edit them. Resume only after an operator Windows sign-out/restart or BricsCAD/license repair makes the official shortcut create a visible responsive main window again. Then reactivate and push this claim first, synchronize the newest `main`, rebuild all exact-SHA gates and rerun the fresh disposable Level probe. Do not close or modify an operator-owned BricsCAD session or drawing.

## 2026-08-14 native startup recovered; operator session active

A newly user-started, no-argument BricsCAD V25 process created a visible responsive `BricsCAD Ultimate` main window and opened an operator-owned drawing. This proves the earlier native startup-without-window blocker has cleared without a repository or runner change. The process and drawing are strictly out of scope and this worker did not close, modify or interact with either.

Keep this claim `BLOCKED` only while the operator-owned session remains active, because the isolated Level runner requires zero pre-existing BricsCAD processes. After the operator saves and closes every BricsCAD session, re-fetch current `main`, reactivate and push this claim before any renewed qualification, rebuild the newest exact SHA and rerun the fresh disposable Level probe. Do not run the probe concurrently with the operator session.

## 2026-08-14 operator session closed; qualification resumes

The operator-owned BricsCAD session closed normally and a fresh process audit found zero BricsCAD processes. The checkout is clean and synchronized with `origin/main@b0c4122575b2c29a49a381591252c9c669734fbf`; current ACTIVE/BLOCKED claims do not reserve Level placement, the Level runner or LOCAL-003 evidence.

This reactivation-only status must be visible on `origin/main` before qualification restarts. After publication, fetch and audit again, require a newest clean exact SHA, run full Core Release/smoke, all nine focused Level/static gates and the installed-reference V25 `Release|x64` build, verify matching plugin/Core `ProductVersion`, then run a fresh disposable Level probe. Do not run concurrently with any newly opened operator BricsCAD session, absorb unrelated source defects or dispatch GitHub Actions.

## 2026-08-14 current-main Room Finish smoke blocker handoff

Qualification on clean exact SHA `497b1936792fd0194494896128628fc4de08bf15` passed the Core Release build with `0 warnings / 0 errors`, then the mandatory full Core smoke failed in `RoomFinishFamilyCategorySmoke.MissingFamilyPreservesFallbackBehavior`. The newly merged smoke expects a missing Family reference to survive as a fallback, while the canonical shared `ReportingProjectIdentityGuard.RequireExistingFamilyReference(...)` fails closed first for the same `MISSING` FamilyId. The sanitized non-local handoff is GitHub issue `#1101`.

This is a CAD-independent source/test contract conflict. This local worker did not edit Core or the smoke, and the focused Level gates, V25 build and native Level probe were not run after the mandatory full-smoke prerequisite failed. Keep this claim reserved but `BLOCKED` until a non-local fix makes the complete Core smoke pass on current `main`; then reactivate/push the claim before rebuilding all exact-current gates and running a fresh disposable probe. No GitHub Actions were dispatched.

## 2026-08-14 resumed after operator session closed

The local checkout is synchronized with `origin/main@604b8662be656e5a87fd8b43769526b1debec7b5`, issue `#1092` remains closed with fresh deterministic smoke evidence, and a new process audit found no running BricsCAD process. Newly added active claims were read and do not reserve Level placement, the Level runner, or LOCAL-003 evidence.

This status-only reactivation must reach `origin/main` before qualification resumes. After publication, re-fetch and require a clean newest exact SHA, full Core smoke, all focused Level/static gates, the installed-reference V25 `Release|x64` build and matching plugin/Core `ProductVersion`; only then create a fresh disposable `*.level-z-probe-copy.dwg` and execute `scripts/test-bricscad-v25-level-z.ps1`. No GitHub Actions are authorized.

## 2026-08-14 exact-current reactivation after source blockers cleared

The local checkout is clean and synchronized with `origin/main@2b66e2e3da1180ba294106ba548f274d91183f2f`. Issues `#990`, `#1092` and `#1101` are closed with their non-local corrections integrated, and the process audit again reports zero BricsCAD processes. Current ACTIVE/BLOCKED claim and open-PR review found no competing owner for Level placement, the Level runner or LOCAL-003 evidence.

This claim is reactivated before any renewed build or native launch. After this status-only commit is visible on `origin/main`, re-fetch the newest clean main, require full Core smoke, all focused Level/static gates, installed-reference V25 `Release|x64` build and matching plugin/Core `ProductVersion`, then execute one fresh disposable Level probe. A new CAD-independent failure returns to a non-local owner; a native failure is recorded only with sanitized evidence. No GitHub Actions are authorized.

## 2026-08-14 issue #1125 remote source-fix split

Clean exact SHA `945f26795725114c33251fc6eca031458e59fd1e` reached licensed BricsCAD V25.2.10 and returned sanitized `LEVEL_Z_RUNTIME_CURTAIN_RANGE_FAILED`: the Level-resolved host occupied `3 m .. 7 m`, panels occupied `3.05 m .. 6.95 m`, but frame output occupied `1 m .. 6.975 m`. The local worker made no builder change and opened source issue `#1125`.

Parent task `/root` explicitly delegated the CAD-independent production correction to `/root/fix_level_curtain_frame_z`. Claim `2026-08-14-codex-issue1125-level-curtain-frame-z.md` exclusively owns `CurtainWallFrameSolidBuilder.cs`, `CurtainWallPathFrameSolidBuilder.cs`, and the minimum deterministic source-contract coverage for this mismatch. This LOCAL-003 claim retains the Level probe/runner, exact-SHA licensed rerun, cleanup evidence, and final runtime status, and will not concurrently edit those production source surfaces while the delegated claim is `ACTIVE`.

## 2026-08-14 issue #1125 Level rebar successor split

The exact `8676b6a8430062931356be7dca3bace268ca233d` licensed rerun verified the corrected Curtain chain and then returned sanitized `LEVEL_Z_RUNTIME_REBAR_FAILED` with complete cleanup. The local worker made no rebar builder change.

Parent task `/root` explicitly delegated the CAD-independent Level-rebar source correction to `/root/fix_level_curtain_frame_z`. Reactivated claim `2026-08-14-codex-issue1125-level-curtain-frame-z.md` owns only the implicated production rebar/mesh builder placement and minimum focused source coverage. This LOCAL-003 claim retains all runtime probe/runner/evidence and will not concurrently edit those successor source surfaces while the delegated claim is `ACTIVE`.

## 2026-08-14 sanitized rebar checkpoint diagnostics

The exact licensed rerun on `5972a5bfda3a20df549e75364b40b0824286f162` verified the corrected legacy/host/Curtain ranges but still returned the coarse `LEVEL_Z_RUNTIME_REBAR_FAILED` code. That code covered longitudinal build/count, stirrup build/count, native range reads and two containment checks, so another production edit was not justified.

Within this already-published LOCAL-003 probe/runner reservation, the harness is narrowed to emit only an allowlisted `rebar_stage`, reached nonnegative result counts, reached finite rebar/stirrup Z ranges, and exception type/target/HResult classification. It must never emit exception message, stack, source/data, IDs, handles, drawing paths or geometry identities. The runner and focused gate validate that allowlist and preserve exact-SHA, disposable-copy and cleanup behavior. Production rebar builders remain read-only until a fresh licensed marker identifies a bounded source defect.
