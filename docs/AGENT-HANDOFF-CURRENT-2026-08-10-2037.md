# QS3D current handoff — 2026-08-10 20:37 UTC+7

This is the short canonical delta for agents continuing from current `main`. Current source wins over older handoff/history text.

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
