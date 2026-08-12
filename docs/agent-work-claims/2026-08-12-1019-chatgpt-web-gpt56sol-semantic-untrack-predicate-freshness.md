# Work claim — Semantic untrack predicate freshness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:19:00+07:00`
- Baseline main SHA: `ee2befc6ea239b20528a6837dd8d1e2ba19161e1`
- Priority: evidence-driven Core caller-callback/project-state freshness

## Confirmed defect

`SemanticUntrackService.Untrack(...)` first resolves semantic ownership, then evaluates the caller-provided optional `predicate` over resolved project-owned elements. The predicate is arbitrary caller code, but the service currently does not pin `ProjectState.ChangeVersion` while that callback executes. A predicate can therefore change the project revision and return a target, after which dependency blockers and untrack mutation continue against a different project revision than the one used for ownership resolution.

This is inconsistent with the lazy-input freshness boundaries already used by Locate, Family assignment, targeted regeneration, and `SemanticHandleOwnershipResolver.Resolve(...)`.

## Intended scope

- pin `ProjectState.ChangeVersion` across predicate evaluation after ownership resolution;
- reject a changed project before dependency blocker planning or semantic removal begins;
- preserve null predicate behavior, stable filtering, target ordering, dependency protection, transactional rollback, and caller-side mutations already performed by the predicate;
- add focused Core smoke coverage for stable predicate and project-mutating predicate.

## Reserved surfaces

- `src/QS3D.Core/Services/SemanticUntrackService.cs`
- `tests/QS3D.Core.SmokeTests/SemanticUntrackPredicateFreshnessSmoke.cs`
- this claim file

## Excluded scope

Do not modify semantic handle ownership resolver, generated ownership policies, dependency graph semantics, CAD selection/UI adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual execution.
