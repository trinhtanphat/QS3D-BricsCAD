# Work claim — Regeneration profile DTO integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `7fc5c6fc00ae80178a753078cc869cf960218861`
- Priority: evidence-driven remote-safe Core profile integrity hardening

## Reason

The public regeneration profiling DTO constructors currently validate only part of their state. `RegenerationWorkItem` accepts undefined `ElementCategory`, dirty-flag bits outside `ElementDirtyFlags.All`, and negative dependency depth/counts; `RegenerationCategoryWork` accepts undefined categories; `RegenerationWorkProfile` accepts undefined `RegenerationWorkScope` values. These invalid DTOs can report misleading work/category/readiness metrics even though the profiler itself only produces valid values.

## Reserved scope

Fail closed at the public regeneration profile DTO construction boundary for undefined category/scope values, unsupported dirty-flag bits, and negative dependency metrics. Preserve profiler algorithms, topological ordering, valid combined dirty flags, valid zero counts/depths, public property types, and all profiler-generated values. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfileDtoIntegritySmoke.cs`
- this claim file

## Excluded scope

- No changes to `SemanticRegenerators.cs`, regeneration execution, dependency graph algorithms, preview/apply, CAD adapters, UI, or BricsCAD V25 runtime.
- No changes to engineering semantics or work ordering.
- No GitHub Actions dispatch.

## Validation plan

- Assert undefined category/scope values fail construction.
- Assert dirty flags with an unknown bit fail while valid combined flags remain accepted.
- Assert negative dependency depth/direct dependency/direct dependent counts fail while zero is accepted.
- Confirm a valid work item still reports semantic dirty work consistently.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The active semantic-number regeneration claim is explicitly limited to `SemanticRegenerators.cs`; this lane is confined to `RegenerationWorkProfiler.cs` public DTO construction and a dedicated smoke. No current/recent work-profile DTO integrity claim was found.

## Completion condition

Current `main` rejects invalid public regeneration profile DTO state without changing profiler-generated valid results, includes focused regression coverage, and this claim is marked `COMPLETED`.
