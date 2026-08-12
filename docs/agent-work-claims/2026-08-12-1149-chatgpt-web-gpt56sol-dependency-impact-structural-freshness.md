# Work claim — Dependency Impact structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-structural-freshness-20260812-1149`
- Registered: `2026-08-12T11:49:00+07:00`
- Baseline main SHA: `86ed1ecf7ce2189f9ba64b35354dea6f0fb695b4`
- Priority: P1 — read-only impact planning must not silently switch to a different semantic element structure during caller-controlled enumeration.
- Task Key: `CORE-DEPENDENCY-IMPACT-STRUCTURAL-FRESHNESS`

## Confirmed defect

The completed Dependency Impact input-freshness lane moved the `ChangeVersion` snapshot before lazy root enumeration, so ordinary semantic mutations that call `ProjectState.Touch()` are detected. However `ProjectState.Elements` remains a public mutable list. A caller-provided lazy `sourceElementIds` sequence can directly add/remove/reorder/replace entries while it is enumerated without advancing `ChangeVersion`. `DependencyImpactPlanner.Plan(...)` then rebuilds `DependencyGraph` from the changed collection and can return a plan carrying the unchanged pre-enumeration `SourceChangeVersion`, even though graph structural identity changed inside the planning window.

## Reserved scope

- `src/QS3D.Core/Services/DependencyImpactPlanner.cs`
- `tests/QS3D.Core.SmokeTests/DependencyImpactStructuralFreshnessSmoke.cs`
- this claim file

`DependencyGraph.cs` is explicitly excluded; PR #832 self-dependency validation is already integrated independently.

## Intended contract

- Snapshot the exact ordered semantic element object references before caller root enumeration.
- After caller enumeration and before graph rebuild, reject if semantic element count/order/reference identity changed even when `ChangeVersion` did not.
- Re-check the same structural snapshot after graph traversal before returning the plan, so direct structural edits from any later caller/reentrant path cannot be silently accepted.
- Preserve existing `ChangeVersion` freshness, root cardinality/canonicality/duplicate/missing validation, deterministic traversal and read-only output API.
- Do not convert direct collection edits into mutations or rewrite `DependencyGraph`.

## Validation plan

Add focused auto-registered Core smoke coverage where a lazy root enumerable replaces a non-root project element with a new instance carrying the same ID without calling `Touch()`. The planner must reject before returning a plan while `ChangeVersion` remains unchanged. Include a stable control proving ordinary impact planning still succeeds.

## Validation boundary

No GitHub Actions will be dispatched. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
