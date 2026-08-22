# QS3D current handoff — 2026-08-10 20:37 UTC+7

This is the short canonical delta for agents continuing from current `main`. Current source wins over older handoff/history text.

## Superseding review delta — 2026-08-10 23:10 UTC+7

This section is newer than the historical status paragraphs below and supersedes them where they conflict with current source.

A broad source review of the fast-moving `main` re-checked repository policy, current open product gaps, recent commits, the two stale-but-substantive draft PRs #165/#173, persistence/quantity safety, modeless UI lifetime work and smoke-test discoverability. The review deliberately reused validated source slices instead of blindly merging stale branch history or overwriting concurrent commits.

Three concrete correctness regressions were selected for the request-scoped integration batch:

1. **DWG ↔ QSDB persistence lifecycle.** Pending in-memory semantic mutations are tracked by monotonic `ProjectState.ChangeVersion`. Successful native DWG Save/SaveAs can persist pending state to the matching `.qsdb`; close with pending semantic state requires an explicit Save/Discard/Cancel choice; sidecar-save failure vetoes close and attempts a detached LocalAppData recovery copy. Snapshot/QSDB failure rollback restores both timestamp and change version. Lifecycle handlers are detached before project context is forgotten. The source implementation landed concurrently on `main`; this review batch retains/extends its missing regression coverage instead of duplicating the winning source commit.
2. **Drawing-unit / B4D quantity safety.** Undefined or unsupported `INSUNITS` no longer silently becomes millimeter for unit-dependent recognition/capture/BQ/ED2/reconcile. `QS3DUNITS` provides an explicit persisted project override only when native `INSUNITS` is unavailable; known native units remain authoritative. Existing semantic quantities are bound to their effective capture unit and mismatches fail closed instead of being silently rescaled.
3. **Proxy/BRC capture safety and smoke compile regression.** Metricless `ProxyEntity` candidates remain visible for review but are excluded from auto-accept/capture until a finite positive category-appropriate primary metric exists. `AtomicFileCommitFallbackSmoke` also restores the missing `System.Linq` import required by its `.Any()` assertion.

The unit state is surfaced in Project Tools together with a `QS3DUNITS` launcher. New smoke/static guards cover persistence stamps/change-version rollback, save lifecycle, drawing-unit resolution and ProxyEntity capture eligibility. Existing concurrent Project Tools commands and smoke registrations from newer `main` are preserved.

The source slices reused from draft PR #173 had prior branch evidence of Core smoke PASS, aggregate preflight 232/232 PASS, BricsCAD V25 x64 Release compile with 0 warnings/0 errors, and offline WPF checks PASS. The source slices reused from draft PR #165 had prior branch evidence of aggregate preflight 221/221 PASS, Core smoke PASS, BricsCAD V25 x64 Release compile with 0 warnings/0 errors, and offline WPF checks PASS. **Those are branch results, not a claim that the newly integrated exact `main` commit has been executed in CI or licensed BricsCAD.** GitHub Actions remain undispatched because the owner requested review/fix/commit/push, not CI/build/release. Exact-SHA interactive Save/SaveAs/Close, NETLOAD/DemandLoad and native V25 behavior remain LOCAL_ONLY.

Current Interchange source is also newer than the older paragraph below: `QS3DINTERCHANGEIMPORT` routes explicit append/KeepTarget/guarded Element replacement policy slices; `QS3DINTERCHANGEUSESOURCECATALOG` provides a separate guarded Zone/Floor/Family source-semantic replacement slice; and source-handle provenance has a provenance-only path. These still do not imply arbitrary rename/remap, native source-handle ownership adoption or automatic physical rebuild/cut.

Current documentation/model-health source is newer as well: authoritative native BBS Table source has landed, and deterministic Core semantic saved-view/sheet planning has landed. Native Layout/Viewport/title-block materialization and exact visual/runtime qualification remain separate LOCAL_ONLY/product work.

## Owner commit policy

The owner explicitly requires **request-scoped commit batching**. Treat one owner request / `continue all` as the default coherent commit unit: accumulate related source, regression/static guards, docs and handoff, review them together, then commit. Do not create file-by-file or tiny-fix commit chains. Split only for genuinely independent/revertable risk, an already-independent PR (prefer squash), or conflict-safe integration forced by concurrent `main` movement. Never force-push newer agent work away.

## Interchange — source-safe pipeline now

Current source provides deterministic Semantic Snapshot v1 export, strict read-only validation, immutable typed reading, semantic diff/collision preview and explicit import-resolution policy planning. Current `main` also contains the first deliberately narrow mutating command `QS3DINTERCHANGEAPPEND`: append-only, all-new semantic identities, explicit confirmation, target-authority preservation, source CAD Handle discard, generated/native ownership discard, provenance/audit and semantic rollback.

Append-only is **not** generic import authority. There is still no broad `QS3DINTERCHANGEIMPORT` merge/replace engine. Collision execution (`KeepTarget` / `UseSourceSemanticData`), rename/remap, generated-output clearing/rebuild for replacements, project/drawing identity policy execution and exact-SHA V25 qualification remain separate reviewed work.

Portable Semantic Snapshot interchange still does not serialize drawing-local `ProjectState.Metadata`; native documentation Table handles/positions remain `.qsdb`-local state.

## Documentation / model health

Current source contains:

- native semantic MText tag create/refresh/remove plus persisted/live health;
- project-owned generic Semantic Element native Table;
- authoritative Door/Opening native Table from `DoorOpeningScheduleBuilder`;
- authoritative Room Finish native Table from `RoomFinishScheduleBuilder`;
- authoritative Material Usage native Table from `MaterialUsageScheduleBuilder`;
- authoritative BQ native Table from `ProjectQuantityReportBuilder`, with create/refresh regeneration and all 19 `XlsxQuantityExporter` quantity + traceability columns.

Project-level Tables use dedicated `QS3DDOC` ownership and distinct artifact IDs/metadata prefixes rather than dummy semantic element ownership. `.qsdb` persists their metadata; portable interchange excludes it. Runtime providers are fail-isolated and the shared runtime-health aggregator is consumed by `QS3DRELEASECHECK`.

`docs/COMMANDS-NATIVE-DOCUMENTATION-TABLES.md` is the low-conflict command addendum. The Schedule Hub exposes generic, BQ, Door/Opening, Room Finish and Material Usage Table creation. BBS native Table, MLeader/associative tags, richer TableStyle and Layout/Sheet/Viewport/title-block workflows remain open.

Keep source status and runtime status separate: landed native source is real, but exact visual/interactive V25 behavior is not remotely qualified.

## Grid/Floor/Level/polygon

Extend existing canonical domain models rather than invent parallel stores. Current source includes Grid semantic naming/ordering/intersection/system planning, Floor/Level identity work and polygon-region/mesh planning. Native materialization/constraints/host integration and private-DWG behavior remain separate where documented.

## Remote vs local boundary

Remote agents should continue deterministic Core/domain/persistence/reporting/source-hardening work and prepare probes/tests/docs for local agents. Do not repeatedly re-audit gates already classified LOCAL_ONLY.

Real NETLOAD/DemandLoad, private-DWG workflows, native DrawJig/UI/performance, exact engineering-standard qualification, production signing/timestamp and clean-machine installer proof remain LOCAL_ONLY or owner-policy gated. Use the local handoff documents and exact-SHA runner; do not manufacture `LOCAL_PASS` from source/static evidence.

## CI / release boundary

`continue all`, source review, commit/push, PR merge and handoff updates do **not** authorize GitHub Actions. All workflows remain manual-only. A separate explicit owner request is required before build/runtime/release dispatch.

## Continue-next remote priorities

Before writing, sync latest `main`/open PRs and reuse sound concurrent work. For documentation, BBS is the next clearly source-resolvable authoritative native Table candidate; do not duplicate BBS fabrication logic—consume `ProjectRebarScheduleBuilder` only and preserve fabrication qualification guards. For Interchange, do not widen append-only into merge/replace without dependency-ordered mutation, ownership reset/rebuild, rollback/audit and explicit user-confirmed policy execution.
