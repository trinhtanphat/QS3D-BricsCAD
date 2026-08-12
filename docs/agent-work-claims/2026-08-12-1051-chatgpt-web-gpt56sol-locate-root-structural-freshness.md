# Work claim — Locate root structural freshness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:51:00+07:00`
- Baseline main SHA: `9110226555dd310daa8188969ab543dfe74bb0a6`
- Priority: evidence-driven Core Locate caller-input/project-ownership freshness

## Confirmed defect

`SourceHandleResolver.Resolve(ProjectState, IEnumerable<string>)` pins `ProjectState.ChangeVersion` while materializing caller-provided lazy root element IDs, then builds the project element index used by Locate. `project.Elements`, however, is a publicly mutable list. A lazy root enumerable can directly remove or replace a selected `ProjectElement` instance without calling `ProjectState.Touch()`, so `ChangeVersion` remains unchanged and Locate can silently resolve the root ID against a different structural ownership state.

The sibling `SemanticHandleOwnershipResolver.Resolve(...)` has already been hardened against the same direct-list structural replacement class by snapshotting element-ID → exact-instance ownership before caller enumeration and revalidating it afterward.

## Intended scope

- snapshot and validate current project element identity/instance ownership before caller root-ID enumeration;
- preserve the existing `ChangeVersion` freshness check across enumeration;
- after enumeration, fail closed if element count/IDs/exact instances changed even when `ChangeVersion` did not;
- preserve current root-ID normalization/bounds, missing-root behavior, dependency validation, room-finish traversal, direct/boundary/generated handle fallback and deterministic output;
- add focused Core smoke coverage for stable lazy roots, direct removal, same-ID replacement and mutating-empty enumeration.

## Reserved surfaces

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleResolverStructuralFreshnessSmoke.cs`
- this claim file

## Excluded scope

Do not modify semantic-handle ownership resolver, quantity-report selection freshness, dependency graph semantics, CAD/UI adapters, generated-handle policies, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim executable .NET smoke/build or BricsCAD V25/V26 runtime PASS without actual execution.
