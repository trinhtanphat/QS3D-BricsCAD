# QS3D goal 100% — remaining work and closure gates

Updated: 2026-08-12 (UTC+7)

Audit baseline for this synthesis: `53f3b93452265eef4add40e633f2a74cd790a7f3`.

This document is a compact execution index for the owner goal of reaching a defensible **100%**. It does not replace current source, GitHub issues, `docs/PRODUCT-BOUNDARY.md`, `docs/REMOTE-AGENT-SCOPE.md`, `docs/LOCAL-AGENT-INBOX.md`, or host qualification runbooks. `main` changes rapidly; newer source and explicit issue/inbox status always win over this snapshot.

## What “100%” means

QS3D may be called 100% complete only when all applicable layers below are closed for the intended shipping host(s):

1. **Source/static completeness** — explicit product contracts are implemented, bounded, fail-closed, regression-guarded, and no known reproducible source defect remains open.
2. **Host runtime qualification** — the exact release SHA is built and exercised on the matching licensed BricsCAD major with native CAD, save/reopen, Undo, multi-DWG, UI, installer/update and release evidence as required.
3. **Policy/engineering closure** — any commercial licensing policy, structural/fabrication standard, ownership behavior or other owner/engineering decision has been explicitly selected before code claims compliance.
4. **External-format closure** — any promised interoperability scope (for example IFC/Revit/BCF/vendor/cloud) has an explicit supported-format/version contract plus tests and runtime evidence.

Source presence alone is not 100%. A V25 runtime PASS is not V26 evidence, and remote/static evidence cannot manufacture a local BricsCAD PASS.

## Current completion shape

The repository is beyond prototype/MVP and has broad source coverage for semantic BIM/QS data, `.qsdb` lifecycle, Direct Draw and Plan-to-3D, Room/Finish, Door/Opening, Curtain, quantity/BQ/XLSX, schedules/documentation, rebar, health/readiness, updater/release tooling, V25 and V26 adapters.

The remaining product gaps are now dominated by native host qualification, advanced CAD behavior, engineering/policy decisions and external-format scope rather than missing foundational architecture. Remote agents should continue fixing concrete source defects, but must not invent native geometry, code-compliance values, licensing rules or interchange semantics merely to make the checklist appear complete.

## P0 — release blockers

### A. Exact BricsCAD V25 candidate qualification — issue #72 / LOCAL-001 and linked LOCAL items

Classification: `LOCAL_ONLY` / `DO_NOT_RETRY_REMOTE`.

Already present:

- V25 `net48/x64` adapter and manual source/build/runtime tooling.
- Large deterministic Core smoke/preflight surface.
- Partial exact-SHA V25 build/load/lifecycle evidence recorded in `docs/LOCAL-AGENT-INBOX.md`.

Still required before V25 production completion:

- lock a stable candidate SHA after source churn;
- build Core and V25 adapter against the installed licensed V25 references;
- finish the current `LOCAL-001` interactive command/lifecycle matrix that remains `IN_PROGRESS`;
- close the linked native geometry, Curtain, Level/Grid, documentation, edit, UI and performance LOCAL items that apply to the shipping claim;
- prove save/reopen, Undo/Redo where required, multi-DWG isolation and representative failure rollback on the exact candidate;
- retain only sanitized evidence in Git.

Closure evidence: exact SHA, host version, build identity/hash, command/runtime results and all required LOCAL items `PASS` for the intended V25 release scope.

### B. Exact BricsCAD V26 qualification

Classification: `LOCAL_ONLY` / `DO_NOT_RETRY_REMOTE`.

Already present:

- distinct `QS3D.BricsCAD.V26.dll` on `net8.0-windows`;
- shared adapter source with V26-specific host/update/release boundaries;
- V26 package, installer/update, signature/finalization and runtime helper source.

Still required:

- licensed BricsCAD V26 x64 + .NET 8 Windows Desktop exact-SHA build;
- V26 NETLOAD/DemandLoad and representative CAD/semantic/quantity/generated-geometry matrix;
- save/reopen, cold-cache and two-DWG modeless UI isolation;
- WPF/Ribbon/palette/DPI/shutdown proof under the V26 .NET 8 host;
- clean install/update/rollback/uninstall with V26-only registry/package identity;
- real signing/timestamp/update-channel proof for a production candidate when approved credentials exist.

Closure evidence is defined by `docs/LOCAL-V26-QUALIFICATION.md`. Until then V26 is source/build/package/update implemented with local production qualification pending.

## P1 — native product gaps that need host proof

### #73 Advanced wall geometry / multi-owner physical junction output

Classification: `LOCAL_ONLY / COORDINATED` for remaining physical output.

Already present: deterministic Core ownership/rebuild identity contracts and broad wall/host geometry infrastructure.

Still required: dedicated L/T/X/Multi physical output/replacement/unmerge behavior that preserves all semantic owners and Opening host semantics, richer WallPier/freeform cases, rollback/foreign-ownership refusal, save/reopen and exact-V25 representative geometry proof. Do not implement by arbitrary boolean union.

### #74 Direct Draw transient preview and repeated authoring

Classification: `LOCAL_ONLY`.

Already present: broad P0/P1 Direct Draw source and guarded atomic source→semantic→native flows.

Still required: real DrawJig/transient preview, OSNAP/ORTHO/dynamic-input interaction, repeated authoring mode, ESC residue cleanup and native Undo behavior verified in V25. Do not create a second geometry model.

### #77 Documentation layer native completion

Classification: current Core/tag/table/custom-schedule source is `REMOTE_DONE / SOURCE_IMPLEMENTED`; remaining native document production is `LOCAL_ONLY`.

Still required for a full documentation claim: MLeader/leader behavior if in shipping scope, real TableStyle/format rendering, Layout/Sheet/Viewport/title-block/PaperSpace scale/lock lifecycle, save/reopen/Undo/Unicode/HiDPI/multi-DWG host proof.

### #79 Grid/reference model and Level consumption

Classification: deterministic Grid/Level Core planning is substantially `REMOTE_DONE`; remaining host workflow is `LOCAL_ONLY / COORDINATED`.

Still required: pair-owned native intersection marker lifecycle, rectangular/radial native materialization, reviewed interaction/UI, constraints/dimensions/snapping/hosting where promised, PaperSpace annotations, and coherent Floor-as-Level consumption through host solids/openings/Curtain/rebar with exact-SHA save/reopen/Undo/multi-DWG evidence. Do not introduce a duplicate Level store.

### #80 Native modify/edit workflow

Classification: source reconcile is `REMOTE_DONE`; richer interaction is `LOCAL_ONLY`.

Still required: authoritative MOVE/ROTATE/STRETCH/grip/jig semantics, clear source-vs-generated selection UX, provenance/ownership preservation, Undo/document-switch/save-reopen behavior and rollback proof. Do not create a competing editable geometry model.

### #82 Real-host UI/DPI/Ribbon/context-menu polish

Classification: `LOCAL_ONLY`.

Still required: real screenshots/runtime observations at 100/125/150/200% DPI, narrow/normal/wide palettes, Vietnamese/long text, keyboard focus, ComboBox/popups, disabled/read-only/selected states, Ribbon grouping/icons/context menus and splitter persistence. Visual changes must be driven by host evidence rather than remote guesses.

### #83 Generalized polygon Slab/Foundation native reinforcement

Classification: Core polygon outer+holes+multi-region planning is `REMOTE_DONE`; native materialization is `LOCAL_ONLY`; fabrication behavior remains `ENGINEERING_REQUIRED`.

Still required: native outer/hole/multi-region source identity, straight/bulged extraction and OCS/WCS behavior, owner/reconcile/stale/Health per region, native bar materialization and exact-SHA Undo/save-reopen/multi-DWG proof.

## P1 — release/engineering/product-decision gaps

### #75 Production signing, clean distribution and licensing

Classification:

- signing/install/update/rollback/uninstall: `LOCAL_ONLY / EXTERNAL_CREDENTIAL_REQUIRED`;
- commercial licensing/team/seat behavior: `POLICY_REQUIRED`.

Still required for signed production distribution: approved Authenticode certificate, trusted timestamp, exact-package signature verification, clean-machine install/upgrade/rollback/uninstall, SECURELOAD/DemandLoad proof and owner-authorized release publication.

Commercial licensing must remain policy-gated until SKU/trial/subscription, seat/user/machine binding, offline grace, activation/backend, recovery and key-rotation rules are explicitly decided. Do not invent those rules to close the issue.

### #76 Fabrication-grade rebar/detailing

Classification: `ENGINEERING_REQUIRED / LOCAL_ONLY`.

Already present: broad standards-neutral 3D rebar families, schedules/BBS and fabrication provenance infrastructure.

Still required for any code-compliance/fabrication-grade claim: explicit governing standard + revision, engineering-approved hook/bend/lap/anchorage/detailing-zone behavior and then native geometry/schedule/runtime qualification. Provenance metadata is not compliance certification.

### #84 Broader interoperability/import-export

Classification:

- current semantic snapshot/import policies and dedicated FieldMerge source: `REMOTE_DONE / SOURCE_IMPLEMENTED` for the explicit current contracts;
- FieldMerge native transaction/failure/Undo/save-reopen/multi-DWG proof: `LOCAL_ONLY / COORDINATED`;
- target-DWG source-handle rebinding/adoption: `POLICY_REQUIRED + LOCAL_ONLY`;
- IFC/Revit/BCF/vendor/cloud formats: `FORMAT_SCOPE_REQUIRED`.

Still required for a broader interoperability claim: finish exact-V25 native FieldMerge proof and generated-output rebuild workflow, explicitly choose any handle-adoption policy, and define each additional external format/version before implementation. Do not imply round-trip BIM interoperability beyond supported contracts.

## P2 — performance and release quality

### #81 Large-model performance

Classification: Core harness is `REMOTE_DONE`; representative native profiling/optimization is `LOCAL_ONLY`.

Still required: measure real room topology, junction/Auto Host candidate sets, Curtain grids, BQ/schedules, SPLINE sampling, ownership registries, regeneration, documentation and rebar/mesh batches on representative/private V25 projects. Record native/editor/database time and memory before optimizing; never weaken correctness caps just to improve a benchmark.

## Continuous remote-safe hardening until freeze

Even while the major gaps above await local/policy/engineering input, remote agents should continue evidence-driven bug work on current `main`:

- null/malformed persisted state that can produce false-clean or fail-open behavior;
- finite/overflow/count/spacing/bounds defects;
- canonical identity/order/duplicate handling;
- mutation atomicity, stale-project/document affinity and rollback identity;
- bounded lazy input/enumeration contracts;
- XML/XLSX/JSON schema/input integrity and output sanitization;
- generated ownership/health/readiness integrity;
- deterministic tests and static preflights for each confirmed defect.

Do not create speculative parallel architectures or broad feature rewrites without a demonstrated current-source gap and an ACTIVE claim.

## Source defect closed by the 2026-08-12 goal-100 audit batch

`ProjectQuantityReportBuilder.Group/Detail(project, elementIds)` accepted a lazy caller enumerable and silently ignored duplicate IDs. A source that repeated one valid ID indefinitely could therefore keep `ResolveSelection()` enumerating forever. The batch owning this snapshot changes the contract to require canonical case-insensitive unique selection IDs, preserving blank/unknown refusal while making duplicate input fail closed immediately, and adds focused lazy-enumeration regression coverage.

This is a remote-safe Core fix only; it does not change the LOCAL_ONLY qualification status above.

## 100% closure checklist

Before the owner or release documentation says **100% / production complete** for a host major, all applicable statements below must be true:

- no known reproducible source defect remains unowned/open for the declared release scope;
- aggregate source/static/Core gates pass on the exact candidate SHA;
- matching host adapter builds against the licensed installed major;
- required LOCAL items are `PASS` on that exact SHA with sanitized evidence;
- native geometry/edit/Undo/save-reopen/multi-DWG scenarios for declared features pass;
- UI/DPI/Unicode/runtime behavior for declared UX passes;
- installer/update/rollback/uninstall passes on clean supported machines;
- production signing/trust passes when signed distribution is claimed;
- engineering-required reinforcement rules have explicit approved standard/revision or remain clearly outside the release claim;
- licensing behavior has explicit owner policy or remains outside the release claim;
- external formats have explicit versioned scope and evidence or remain outside the release claim;
- V25 and V26 are qualified independently; evidence is never copied across host majors;
- release publication is explicitly owner-authorized and remains manual-only under `CI_POLICY.md`.

Until those conditions are met, report the repository as feature-rich/source-advanced with the remaining classifications above rather than using a false 100% production claim.
