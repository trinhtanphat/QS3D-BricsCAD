# QS3D BricsCAD V25 — remote implementation completion audit

Updated: 2026-08-11 (UTC+7)

Repository: `trinhtanphat/QS3D-BricsCAD`

Baseline observed immediately before this audit: `86a876faf4f770a0f846e1a057326bf0f0cdffea`

This document is the completion handoff for the current **remote/source-safe** implementation wave. It does **not** claim the complete product is finished. `main` is still being modified concurrently; current source always wins over this snapshot.

## 1. Completion statement

The repository has reached the point where the remaining open product-gap issues are no longer well-served by indiscriminate remote feature generation.

The major deterministic/Core gaps that had clear contracts have been implemented or materially advanced. Remaining work is now dominated by one or more of:

- `LOCAL_ONLY`: licensed interactive BricsCAD V25/native API/runtime evidence is required;
- `POLICY_REQUIRED`: owner/product/commercial behavior must be selected before code can be correct;
- `ENGINEERING_REQUIRED`: a governing engineering standard/revision and engineering approval are required;
- `FORMAT_SCOPE_REQUIRED`: external interoperability schema/vendor/product scope must be explicitly selected.

Remote agents must not turn these boundaries into fake completion by inventing native behavior, unreviewed merge precedence, engineering design values, licensing policy or signing credentials.

## 2. Source-safe implementation delivered in this wave

### Interchange / issue #84

The following coherent batches were merged:

- `ada6d3bf6957392520e61421622963aba451295a` — guarded Core `UseSourceSemanticData` replacement with target-only invalidation, native-cleanup authorization and semantic rollback.
- `4201a933715a0dc352a3bb4a0c6860ea1a518a58` — append-only semantic import composed atomically with canonical non-owning source-handle provenance.
- `05ae3c673e81f04c0d2b05db83473d683520d0be` — Import As New/remap provenance plus bounded source-Element -> target-Element semantic lineage.
- `174366e153a39264d401f11b49ff307acac04550` — UseSource + provenance while preserving the exact native-cleanup authorization boundary.
- `faf30228187d034dd06ac9cea0576de58997a9a9` — KeepTarget + provenance, with no false target lineage for source identities rejected by KeepTarget collision policy.
- `a7443d25936a85f6a9b18dd4e16b880f084e0844` — unified Core import coordinator for explicit AppendOnly / KeepTarget / ImportAsNew / UseSourceSemanticData modes plus reviewed provenance selection; no implicit policy fallback.
- `0ff97a919e7a2bdf6e7bd7f17d8232cab7cc0cce` — deterministic same-ID field-level precedence execution with target/source/decision freshness, exact generated-handle cleanup requirements and `ProjectStateSnapshot` rollback; generic BricsCAD orchestration remains intentionally separate.

The coordinator is now the preferred source-level policy entry point for the four generic import modes. The field-level planner/executor is a separate reviewed Core boundary and is intentionally not exposed as a fifth generic coordinator mode.

Important boundaries preserved:

- imported source handles never become target `ProjectElement.SourceHandles` merely because provenance is retained;
- imported source drawing fingerprints never become target Element ownership;
- provenance metadata remains separate from portable semantic Element ownership;
- UseSource and field-level execution retain exact reviewed generated-handle cleanup requirements where affected target owners contain generated/native output;
- Core does not erase or rebuild BricsCAD entities;
- no requested import policy silently falls back to another policy.

### Polygon/multi-region reinforcement / issue #83

Merged:

- `66e19be3f7c79890d79b8ff47adbbc5d59b631a7` — explicit disconnected polygon-island topology with stable RegionId, independent outer+holes, tagged scanline output and fail-closed overlap/touch/nesting behavior.
- `c524858814cbc297e8b93ee7de33fdd5ea0cbcb6` — standards-neutral polygon mesh planning independently per stable RegionId using the canonical single-region hole/cover-aware planner.

The Core model no longer needs to fake disconnected Slab/Foundation islands by concatenating vertices or treating islands as holes. Count-mode spacing remains per region instead of being silently computed over a combined disconnected span.

Native source-loop ownership/materialization is still a separate runtime problem.

### Semantic operation reliability / WS-03 + WS-35

Merged:

- `58e883a32e132e0982a10da5a4734b68a2edf836` — `ProjectSemanticMutationExecutor` with bounded detached phase journal, `ProjectStateSnapshot` rollback, optional pre-commit validation/fault injection and rollback-failure aggregation.

The regression suite includes a real composed interchange mutation followed by an injected pre-commit fault and verifies semantic elements, provenance metadata, audit, `UpdatedUtc` and `ChangeVersion` return to the captured state.

This is semantic rollback only. It does not convert native DWG side effects into a transaction.

### User-defined semantic documentation / issue #77

Merged:

- `3d0dd8bbd3df3450f1baf97c97076c468af230fb` — persisted bounded `SemanticScheduleDefinition` catalog with deterministic category/Floor/Zone/include/exclude filtering and rendering through the existing authoritative semantic documentation table/tag renderer.

Custom schedules do not become a second BQ/BBS calculator. `{Q:...}` displays existing semantic quantities; authoritative BQ/BBS/Room/Material/Door-Opening calculators remain separate.

## 3. Current open-issue classification

### #72 — exact BricsCAD V25 SHA qualification

Status: `LOCAL_ONLY`.

Remote implementation is not a substitute for this issue. Completion requires interactive Windows + licensed BricsCAD V25 x64 against one locked exact candidate SHA, including installed SDK references, NETLOAD/DemandLoad, native geometry, save/reopen, multi-DWG, UI and installation evidence.

### #73 — advanced geometry / multi-owner wall output

Status: `LOCAL_ONLY / COORDINATED` for remaining physical geometry.

Do not implement physical L/T/X/Multi wall output by arbitrary boolean-union of owner wall solids. Remaining output must preserve original wall ownership/host semantics and be proven with real V25 failure/rollback cases. Richer WallPier/freeform geometry is also native/runtime constrained.

### #74 — Direct Draw preview/repeated authoring

Status: `LOCAL_ONLY`.

P0/P1 Direct Draw source is already broad. Transient thickness/profile preview, DrawJig/editor lifecycle, OSNAP/ORTHO/dynamic input interaction, ESC cleanup, repeated mode and native Undo need licensed V25 runtime evidence. Do not create a second Direct Draw geometry model remotely.

### #75 — signing/install/update/licensing

Status:

- signing/install/update/rollback/uninstall: `LOCAL_ONLY / EXTERNAL_CREDENTIAL_REQUIRED`;
- commercial licensing: `POLICY_REQUIRED`.

Production Authenticode trust cannot be proven without the approved production certificate/timestamp path and clean-machine tests. Licensing must not be invented until SKU, trial/subscription, seat/user/machine binding, offline grace, activation/backend, key rotation and recovery policy are selected.

Never commit signing/license private keys or secrets.

### #76 — fabrication-grade reinforcement

Status: `ENGINEERING_REQUIRED / LOCAL_ONLY`.

Standards-neutral provenance and geometry infrastructure exist. Fabrication numeric behavior for hooks, bends, laps, anchorage, detailing zones and other compliance claims requires an explicit governing standard/revision and engineering approval. Do not infer code-compliance rules from examples or BLT behavior.

### #77 — documentation layer

Status:

- Core semantic tags/tables/View/Sheet/custom-schedule planning: `REMOTE_DONE` for current defined source contracts;
- native custom Table materialization, Layout/Viewport/title block/PaperSpace workflows and host UI/runtime: `LOCAL_ONLY`.

Do not add another documentation calculator or detached decorative annotations.

### #79 — Grid/reference + Level

Status:

- existing Grid system/intersection/spatial-order Core planning: `REMOTE_DONE`;
- native rectangular/radial materialization, interaction, constraints/dimensions, structure snapping/hosting, PaperSpace Grid annotations and coherent Level consumption across native geometry: `LOCAL_ONLY / COORDINATED`.

The existing Floor model is the Level model. Do not create a duplicate Level store.

### #80 — native modify/edit workflow

Status:

- source reconcile (`QS3DSYNCSOURCE`) and source-authoritative semantic refresh path: `REMOTE_DONE` for the current contract;
- MOVE/ROTATE/STRETCH/grips/jigs, source-vs-generated selection behavior, BricsCAD Undo/document switching/save-reopen behavior: `LOCAL_ONLY`.

Do not create a competing editable geometry model.

### #81 — large-model performance

Status:

- bounded Core harness/regression infrastructure: `REMOTE_DONE`;
- representative private-DWG/native/editor/database performance measurement and optimization: `LOCAL_ONLY`.

Measure first in real V25. Do not weaken correctness/fail-closed limits merely to make benchmarks faster.

### #82 — UI/DPI/Ribbon/context menu

Status: `LOCAL_ONLY` for remaining polish.

Source UI is already broad. Final changes must be based on real screenshots/runtime observations at 100/125/150/200% DPI, multiple palette widths, Vietnamese/long text, keyboard focus, popup states, Ribbon/context-menu behavior and splitter persistence. Preserve CAD viewport dominance.

### #83 — generalized polygon Slab/Foundation mesh

Status:

- Core outer+holes topology + disconnected RegionId topology + per-region standards-neutral mesh planning: `REMOTE_DONE` for current contracts;
- native source-loop identity/association, straight/bulged extraction, OCS/WCS behavior, owner slots per region, native rebar materialization, stale/reconcile/Health, Undo/save-reopen/multi-DWG: `LOCAL_ONLY`;
- fabrication/code-specific detailing: `ENGINEERING_REQUIRED`.

### #84 — broader interoperability/import-export

Status:

- current guarded semantic snapshot execution policies, provenance compositions and unified Core coordinator: `REMOTE_DONE` for the explicit policies implemented on `main`;
- deterministic same-ID field-level precedence planner plus reviewed rollback-safe Core executor: `REMOTE_DONE` for the current explicit group-level policy contract; this is not a fifth generic coordinator mode;
- native replacement/field-merge cleanup and rebuild orchestration, transaction/compensation, Undo/session/save-reopen/multi-DWG and customer/private-DWG qualification: `LOCAL_ONLY / COORDINATED`;
- target-DWG source-handle adoption/rebinding: `POLICY_REQUIRED + LOCAL_ONLY`;
- IFC/Revit/BCF/vendor/cloud formats: `FORMAT_SCOPE_REQUIRED` before implementation.

Do not invent a new or broader field-merge precedence contract beyond the reviewed source model. Current field merge is explicit group-level KeepTarget/UseSource/Unspecified precedence for same-ID semantic collisions; source-only identities remain routed to AppendOnly or ImportAsNew. Per-key mixed policy, native ownership adoption and new precedence categories require their own reviewed product contract before implementation.

## 4. Workstream closure by execution wave

### Wave A — deterministic Core/docs/tests

Current assessment: `REMOTE_DONE` or sufficiently implemented for the explicitly defined contracts. New remote work should be evidence-driven regression hardening, not feature-count expansion.

Includes Interchange Core/coordinator/provenance/field-merge planner+executor, semantic documentation/custom schedules, dependency/performance harnesses, deterministic diagnostics and source-safe fault testing.

### Wave B — coordinated shared platform

Current assessment: substantial source foundation exists. Continue remotely only when a concrete defect or missing deterministic contract is demonstrated on current `main`.

Do not independently rewrite `ProjectState`, ownership policy, regeneration, lifecycle, unit policy or source-reconcile contracts simply to create parallel work.

### Wave C — native V25 product work

Status: `LOCAL_ONLY`.

Includes DrawJig/repeated authoring, native edit/grips, Curtain whole-operation atomicity/panels, physical wall junctions, native Level Z-chain, native polygon-loop/materialization, Layout/Viewport, DPI polish, real performance and exact-SHA qualification.

### Wave D — release/engineering/commercial policy

Status: `POLICY_REQUIRED / ENGINEERING_REQUIRED / LOCAL_ONLY`.

Includes fabrication-standard behavior, licensing product policy, production signing credentials and clean-machine distribution evidence.

## 5. Exact next execution order on a licensed V25 machine

When a local agent with licensed BricsCAD V25 x64 is available:

1. fetch latest `main`, stop source churn for the candidate and record the exact candidate SHA;
2. run source preflights/Core smoke on that exact SHA;
3. build the adapter against the installed V25 `BrxMgd.dll` / `TD_Mgd.dll` references;
4. NETLOAD and DemandLoad on disposable test drawings;
5. run the persistence Save/SaveAs/Close/recovery and multi-DWG matrix;
6. run Direct Draw/UCS and native modify/grip/jig scenarios;
7. run Door/Opening booleans, Room/HT_PHONG, Curtain and wall-junction native failure cases;
8. run Grid/Level/native vertical-placement scenarios;
9. run Slab/Foundation polygon/hole/multi-region + rebar native materialization scenarios;
10. run BQ/BBS/Excel/documentation/Layout/Viewport scenarios;
11. run interchange confirmation, UseSource and reviewed field-merge native cleanup/rebuild/recovery, Undo, save/reopen and multi-DWG scenarios;
12. run UI screenshot/DPI/Unicode/keyboard/focus/Ribbon/context-menu matrix;
13. run representative large/private-DWG performance measurements;
14. only after the candidate remains stable, run clean install/upgrade/rollback/uninstall and production signing checks when approved credentials exist;
15. commit only sanitized qualification summaries; keep private DWGs, proprietary SDK DLLs, raw customer evidence and secrets out of Git.

If source changes after the candidate SHA is locked, qualification evidence belongs to the old SHA and the changed candidate must be requalified where affected.

## 6. CI / release policy

No GitHub Actions workflow was dispatched by this implementation wave.

The repository's manual-only CI/release policy remains unchanged. `continue all`, source implementation, documentation updates and commits do not constitute authorization to dispatch Actions or publish a Release.

## 7. Multi-agent handoff rule

Before any new implementation:

1. fetch current `main`;
2. read the latest issue body and current handoff docs;
3. search for an existing implementation before creating a new class/store/planner;
4. classify the remaining gap as remote-safe vs `LOCAL_ONLY` / `POLICY_REQUIRED` / `ENGINEERING_REQUIRED` / `FORMAT_SCOPE_REQUIRED`;
5. proceed remotely only when the contract is deterministic and independently verifiable from source;
6. re-fetch immediately before integration and never force-push shared history.

The default next step is **local V25 qualification**, not another broad remote `implement all` pass.
