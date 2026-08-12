# Work claim — Regeneration profile subset input freshness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:24:00+07:00`
- Baseline main SHA: `a41cac6f29c28440d21f3fc378ad31a5d5f3afc6`
- Priority: evidence-driven Core caller-input/project-state freshness

## Confirmed defect

`RegenerationPreviewService.PreviewSubset(...)` establishes `SourceChangeVersion` before materializing caller-provided target IDs, and its internal preview path rejects a project revision change before processing. `RegenerationEngine.RegenerateDirtySubset(...)` likewise captures the project revision before target enumeration. `RegenerationWorkProfiler.ProfileSubset(...)`, however, currently calls `CanonicalTargetIds(...)` first and only captures `ChangeVersion` later inside `Build(...)`.

A side-effecting lazy target enumerable can therefore change the project during target materialization and the profiler will accept the post-enumeration revision as its baseline, producing a profile whose target scope was established across a project revision change.

## Intended scope

- capture `ProjectState.ChangeVersion` before `ProfileSubset(...)` materializes target IDs;
- reject a changed project immediately after materialization, including the empty-target path;
- preserve current target canonicality/deduplication/count bounds, null/duplicate project integrity, dependency graph validation, deterministic ordering and full-project `Profile(...)` behavior;
- add focused Core smoke coverage for stable lazy input plus mutating non-empty and mutating-empty input.

## Reserved surfaces

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfilerInputFreshnessSmoke.cs`
- this claim file

## Coordination

The immediately preceding profiler null-entry-integrity claim is `COMPLETED`; this lane starts from its merged source and does not alter DTO null-entry checks. Do not modify Template Profile PR #747, regeneration execution, preview service, dependency graph semantics, UI/CAD adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual execution.
