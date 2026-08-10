# QS3D current handoff — 2026-08-10 20:37 UTC+7

This is the short canonical delta for agents continuing from current `main`. Current source wins over older handoff/history text.

## Owner commit policy

The owner explicitly requires **request-scoped commit batching**. Treat one owner request / `continue all` as the default coherent commit unit: accumulate related source, regression/static guards, docs and handoff, review them together, then commit. Do not create file-by-file or tiny-fix commit chains. Split only for genuinely independent/revertable risk, an already-independent PR (prefer squash), or conflict-safe integration forced by concurrent `main` movement. Never force-push newer agent work away.

## Interchange — source-safe pipeline now

Current source provides:

- deterministic read-only Semantic Snapshot v1 export;
- strict read-only validation;
- immutable validation-first typed snapshot reading;
- deterministic semantic diff;
- target collision preview;
- explicit non-mutating import-resolution policy planning from squash-merged PR #153 (`c108ad135fea78c6bb4367c36635eb429c87e331`).

`ProjectInterchangeImportResolutionPlanner` requires explicit choices for Zone/Floor/Family/element collisions, project ID, drawing fingerprint and drawing-local source Handle provenance. `Unspecified`/unsupported policy values fail closed. Category-incompatible Family/element identities cannot be forced through replacement. Ambiguous target duplicate IDs fail closed. Replacing an existing element from source semantic data requires the planned generated-output reset `ClearOwnershipAndRequireRebuild`; existing native/generated ownership is never trusted automatically.

`CanProceedToMutationDesign=true` is **not import authority**. There is still no `QS3DINTERCHANGEIMPORT`, no `.qsdb`/DWG mutation, no source Handle rebinding, and no automatic ownership clearing. A real importer still needs semantic precedence/catalog rules, dependency-ordered mutation, version migration, canonical ownership reset/rebuild, rollback/audit, explicit UX confirmation and exact-SHA V25 qualification.

## Documentation / model health

Concurrent `main` now contains native project-owned Semantic Element Table plus authoritative documentation tables including Door/Opening, Room Finish and Material Usage, with native health/source guards evolving alongside them. Keep source status and runtime status separate: landed native source is real, but exact visual/interactive V25 behavior is not remotely qualified.

Grid/Floor/Level/polygon work should extend the existing canonical domain models rather than invent parallel stores. Current source includes Grid semantic naming/ordering/intersection/system planning and Floor/Level identity work; native materialization/constraints/host integration and private-DWG behavior remain separate where documented.

## Remote vs local boundary

Remote agents should continue deterministic Core/domain/persistence/reporting/source-hardening work and prepare probes/tests/docs for local agents. Do not repeatedly re-audit gates already classified LOCAL_ONLY.

Real NETLOAD/DemandLoad, private-DWG workflows, native DrawJig/UI/performance, exact engineering-standard qualification, production signing/timestamp and clean-machine installer proof remain LOCAL_ONLY or owner-policy gated. Use the local handoff documents and exact-SHA runner; do not manufacture `LOCAL_PASS` from source/static evidence.

## CI / release boundary

`continue all`, source review, commit/push, PR merge and handoff updates do **not** authorize GitHub Actions. All workflows remain manual-only. A separate explicit owner request is required before build/runtime/release dispatch.

## Continue-next remote priorities

Prefer non-overlapping, source-safe work that moves an explicit contract forward. Before writing code, fetch latest `main` and open PRs because this repository is moving concurrently. Reuse a sound overlapping implementation instead of creating a duplicate. For Interchange, the next mutating step must not be implemented by simply deserializing source data into live state; mutation architecture must first preserve dependency ordering, ownership reset, rollback/audit and user-confirmed policy boundaries.
