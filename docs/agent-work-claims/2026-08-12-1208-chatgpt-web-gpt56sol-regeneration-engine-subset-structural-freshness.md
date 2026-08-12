# Work claim — Regeneration Engine subset structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-engine-subset-structural-freshness-20260812-1208`
- Registered: `2026-08-12T12:08:00+07:00`
- Baseline main SHA: `9e6a5cc371b93a8df6a683775d7b9b59359421f0`
- Priority: P1 — targeted regeneration must not mutate a structurally replaced semantic element under an unchanged ChangeVersion.
- Task Key: `CORE-REGENERATION-ENGINE-SUBSET-STRUCTURAL-FRESHNESS`

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` captures `project.Elements.Count` and `project.ChangeVersion` before enumerating caller-provided target IDs. This catches ordinary semantic mutations that call `Touch()`, but `ProjectState.Elements` remains a public mutable list. A lazy target-ID sequence can directly add/remove/reorder/replace semantic element entries without changing `ChangeVersion`. After enumeration, the method scans live `project.Elements`, resolves targets from the changed structure, and can regenerate/clean a replacement same-ID instance even though target scope was established against the prior structure.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationEngineSubsetStructuralFreshnessSmoke.cs`
- this claim file

## Intended contract

- Snapshot exact project element ID -> instance ownership before caller target-ID enumeration.
- Use the snapshot count for the existing target cardinality bound.
- After target canonicalization and before resolving/mutating targets, reject count/null/duplicate/removal/same-ID replacement drift even when `ChangeVersion` is unchanged.
- Resolve targets from the validated ownership snapshot/project order rather than silently accepting replacement instances.
- Re-check structural ownership immediately before entering transactional regeneration.
- Preserve existing ChangeVersion freshness, canonical/duplicate/missing target validation, dependency validation, project-order target ordering, rollback behavior, regeneration semantics and full-project regeneration behavior.
- Do not change `DependencyGraph`, `RegenerationPreviewService`, public collections, persistence or native/UI code.

## Validation plan

Add focused auto-registered Core smoke coverage using the established Beam regeneration fixture. A lazy target sequence replaces target `B1` with a new same-ID Beam without `Touch()`; targeted regeneration must fail structural freshness, leave `ChangeVersion` unchanged, and not write regenerated quantities. Include a stable subset control proving normal targeted regeneration still succeeds.

## Validation boundary

No GitHub Actions will be dispatched. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
