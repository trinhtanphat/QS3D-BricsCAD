# QS3D current handoff — 2026-08-10 23:06 UTC+7

This is the newest short canonical source delta for agents continuing from `main`. Always fetch current `main` first; current source wins over this text if concurrent commits move ahead.

## LOCAL-003 Level Z-chain source candidate — 2026-08-13

The coherent vertical-placement source chain is now assembled around `ElementVerticalPlacementService` plus one branch-lazy BricsCAD adapter, `CadElementVerticalPlacement`. No-Level geometry preserves the legacy source-Z + `BottomOffsetM` path; Bottom-only uses Level elevation + explicit Level offset with legacy height; Bottom+Top derives the complete range; Top-only, missing/ambiguous Levels, non-finite offsets and invalid ranges fail closed before native ownership mutation.

The candidate covers qualified wall/structural hosts, Door/WallOpening straight/curved cuts and Auto Host, Curtain LINE/path frames/panels/live state, generated rebar/ties/stirrups/mesh/shape placement, effective semantic quantities, generated vertical snapshots and Level-edit stale propagation. The Floor/Level modeless window exposes guarded Bottom/Top/Clear actions while preserving exact bound-project identity across stale document/project replacement.

`LevelReferenceNativeIntegrationPolicy.IsQualified(...)` now means the category may leave the source integration gate; it is not a customer-release verdict. `QS3DLEVELZPROBE` plus `scripts/test-bricscad-v25-level-z.ps1` is the focused exact-SHA native probe. Until that probe and the wider mm/m, full-category, Undo, save/reopen, multi-DWG and private-DWG matrix pass against the same SHA/DLL, `LOCAL-003` remains `IN_PROGRESS / PENDING_LOCAL / NOT_LOCAL_PASS`.

## Superseding Direct Draw productivity delta — 2026-08-11

Owner feedback identifies excessive modeling interaction as the current product bottleneck. Current source therefore uses a **Quick by default / Advanced for exceptions** authoring pattern while preserving the existing semantic/native safety pipeline.

Primary quick commands now include:

- `QS3DDRAWWALL` — two-point straight wall using compatible ArchitecturalWall Family values;
- `QS3DDRAWBEAM` — two-point Beam using Family Width/Height/BottomOffset;
- `QS3DDRAWCOLUMN` — center-point Column using Family Width/Depth/Height/BottomOffset;
- `QS3DDRAWSLAB` — picked closed boundary, then Family Thickness/BottomOffset without post-boundary numeric prompts;
- `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION` — guarded P1 geometry with compatible Family values and no normal numeric prompt sequence after accepted geometry;
- `QS3DDRAWDOOR`, `QS3DDRAWOPENING` — picked two-point width plus Family Height/Sill/Clearance, then the established selection-scoped Auto Host lifecycle;
- `QS3DDRAWWINDOW` — picked two-point width plus WallOpening WindowHeight/Sill/Clearance, `OpeningUsage=Window`, then the same guarded Auto Host lifecycle;
- `QS3DDRAWWALLREF` — read-only reference LINE length/direction plus compatible ArchitecturalWall Family values.

The prior explicit parameter-entry behavior is retained under corresponding `*ADV` commands (`QS3DDRAWWALLADV`, `QS3DDRAWBEAMADV`, `QS3DDRAWCOLUMNADV`, `QS3DDRAWSLABADV`, the four guarded P1 `*ADV` commands, `QS3DDRAWDOORADV`, `QS3DDRAWOPENINGADV`, `QS3DDRAWWINDOWADV`, and `QS3DDRAWWALLREFADV`). The existing primary Ribbon/Domain Hub buttons intentionally keep the primary command names, so the normal UI becomes faster without duplicating rows of buttons.

This wave does **not** introduce a second geometry/model system. It preserves real DWG source provenance, compatible Family lookup, `SemanticCaptureService`, `ProjectStateSnapshot`, `ProjectElement.SetProperty()` where applicable, deterministic regeneration, canonical wall/structural builders or `QS3DBUILD3D`, generated ownership verification, operation-scoped rollback, Door/Opening/Window Auto Host guards and explicit physical opening cutting. The existing `QS3DCONVERT2D` / `QS3DPLAN2WALLS` batch path remains the fast conversion route for pre-existing 2D wall centerlines.

Focused static-contract sources are in:

- `scripts/preflight-quick-wall-authoring.py`;
- `scripts/preflight-quick-structure-authoring.py`;
- `scripts/preflight-quick-p1-authoring.py`;
- `scripts/preflight-quick-opening-authoring.py`;
- `scripts/preflight-quick-window-authoring.py`;
- `scripts/preflight-quick-reference-wall-authoring.py`.

Focused docs are `docs/DIRECT-DRAW-QUICK-*.md`. Exact BricsCAD V25 editor/cancel/Auto-Host/reference behavior, transient DrawJig preview and repeated authoring remain `LOCAL-008 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; current source/static review must not be promoted to runtime qualification.

## Product/source wave added in this continue-all batch

The owner requested a detailed product-logic review plus implementation of meaningful remote-safe features. The source wave deliberately focused on BLT-style review-before-mutation, semantic transaction safety and supportability rather than adding cosmetic features or pretending LOCAL_ONLY native gaps are complete.

### Quantity Rule Preview

`QS3D.Core.Rules.QuantityRulePreviewService` now provides detached element/project dry-runs using the real `QuantityRuleEngine`.

Current source contract:

- Added / Changed / Removed quantity output classification;
- before/after numeric values and `Rule:<Output>` provenance;
- provenance-only stale output removal is visible;
- malformed/ambiguous managed provenance fails closed;
- exact project-owned element instance is required;
- preview carries source `ProjectState.ChangeVersion`;
- tracked project change after preview makes Apply stale even if the resulting delta happens to look equivalent;
- project Apply is snapshot-rollback protected;
- `ApplyProjectWithHealthGuard` rolls back when a new Model Health Error appears.

Adapter source exposes `QS3DRULEPREVIEW` as **read-only only**. Production mutation confirmation/Undo/session UX is still LOCAL_ONLY V25 qualification work.

### Regeneration Preview

`QS3D.Core.Services.RegenerationPreviewService` now runs the real default `RegenerationEngine.RegenerateDirty` on a detached project and reuses `RevisionService` for semantic field/quantity deltas.

Current source contract:

- regenerated-element, changed-element and changed-field counts;
- before/after Model Health diff;
- source `ProjectState.ChangeVersion` binding;
- stale preview rejection;
- apply blocked when preview introduces Health Errors;
- snapshot rollback when live guarded Apply creates new Health Errors.

Adapter source exposes `QS3DREGENPREVIEW` as **read-only only**.

### Model Health baseline / regression diff

`ModelHealthBaselineService` provides deterministic New / Resolved / Persistent issue sets with severity counts, duplicate collapse and cross-project fail-closed comparison. Use it to judge whether an operation introduced regressions rather than only looking at a final aggregate Health count.

### Privacy-safe diagnostic support export

`ProjectDiagnosticSummaryExporter` provides `QS3D.DiagnosticSummary` v1. `QS3DDIAGSUMMARY` exposes the source-side export command.

The summary contains schema/count/category/health-code aggregates only. It excludes project/DWG identity, paths/fingerprints, CAD handles, semantic IDs/names, properties, quantities and health messages. Publication uses `AtomicFileCommit`.

### Interchange export hardening

`ProjectInterchangeJsonExporter` no longer silently drops, trims or case-insensitively deduplicates malformed `sourceHandles` / `dependencies`. Blank/padded/duplicate values fail closed; canonical values are only sorted for deterministic output.

Earlier in the same source wave, interchange Validator / Typed Reader / Preview / Diff were also aligned around canonical IDs/references/keys and explicit timezone handling. Preview uses the typed reader directly; timestamp diff compares the normalized instant rather than raw offset text.

### Family and element category fail-closed boundary

`ProjectFamily`, the reporting/domain `FamilyDefinition`, and `ProjectElement` reject undefined numeric `ElementCategory` values at construction and later category assignment. A rejected setter leaves the previous valid category unchanged.

This closes the in-memory category-integrity gap before persistence/import/browser validation: callers can no longer introduce an invalid family or semantic-element category and rely on a later save/load or diagnostic path to detect it. `ProjectFamilyAssignmentAtomicitySmoke` covers family construction/setter rejection; `ProjectElementCategoryIntegritySmoke` covers element construction/setter rejection. `scripts/preflight-family-category-integrity.py` and `scripts/preflight-element-category-integrity.py` are auto-discovered by the aggregate feature preflight.

The `ProjectElement.Category` hardening intentionally preserves the prior setter side-effect contract: this source batch adds validation only and does not silently introduce new dirty/regeneration behavior for category reassignment. Any future category-change workflow must define that mutation policy explicitly.

This is a Core/source invariant only; it does not change the LOCAL_ONLY BricsCAD V25 qualification boundary.

## Product logic / detailed roadmap

Read `docs/SOURCE-PRODUCT-PLAN-2026-08-10.md` for the new detailed architecture and execution plan.

The intended flow is:

```text
BricsCAD source / Direct Draw
→ semantic ProjectState
→ dirty/dependency graph
→ read-only Rule/Regen Preview
→ explicit mutation decision
→ Core regeneration/rules
→ Model Health regression gate
→ native ownership-safe replacement
→ health/locate/schedules/BQ/BBS
→ interchange/diagnostics/release readiness
```

Do not create a second semantic model, dependency graph, quantity engine or Curtain panel topology planner when an authoritative Core implementation already exists.

## Commands / local qualification

Read:

- `docs/COMMANDS-PREVIEW-DIAGNOSTICS.md`
- `docs/LOCAL-PREVIEW-DIAGNOSTIC-QUALIFICATION-2026-08-10.md`

Local qualification must prove `QS3DRULEPREVIEW` and `QS3DREGENPREVIEW` do not mutate live semantic/native state, prove multi-DWG affinity, inspect `QS3DDIAGSUMMARY` for privacy leakage and qualify explicit confirmation/Cancel/Undo/session behavior before any guarded Apply UX is promoted to production.

## Existing LOCAL_ONLY/product gates remain open

Do not mark these complete from source inspection:

- physical L/T/X/Multi wall-solid reconciliation under safe one-owner semantics;
- Curtain panel-by-panel native glass is now source/static-implemented for guarded LINE/open-bulged paths with opening clipping, independent ownership/stale/health and the six-phase outer transaction; exact V25 geometry/nested-rollback/Undo/save-reopen proof remains LOCAL-002 `PENDING_LOCAL`;
- native generalized polygon/hole loop ownership and disconnected-region policy;
- richer freeform WallPier/native geometry where the current specialized subset is insufficient;
- standard-specific fabrication rebar rules without an approved governing standard/revision;
- exact Windows/BricsCAD V25 build, NETLOAD/DemandLoad, private-DWG, save/reopen/multi-DWG and performance/UI proof;
- production Authenticode certificate/timestamp/signing and clean-machine release qualification.

Use the existing LOCAL_ONLY handoffs; do not repeatedly re-audit them remotely.

## Validation claim boundary

Source smoke and preflight files were added/updated for this wave, including Rule Preview, Regeneration Preview, Health Baseline, Diagnostic Summary and interchange canonical/source-reference behavior.

**No GitHub Actions were dispatched in this source session. Do not claim these new smoke/preflight files were executed, Core/V25 compiled, NETLOAD passed or runtime behavior passed unless a separate exact-SHA execution produces that evidence.**

## Multi-agent discipline

Concurrent agents continue to modify `main`, especially Interchange, UI/document lifetime and native builder boundaries. Re-fetch target files immediately before every write; never overwrite a stale blob or force-push. Reuse newer source work rather than duplicating it.
