# Work claim — Semantic handle selection input freshness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:16:00+07:00`
- Baseline main SHA: `67a7ca73b0fff9c626bfeba7cebdc4c00a50455f`
- Priority: evidence-driven Core caller-input/project-state freshness

## Confirmed defect

`SourceHandleResolver.Resolve(...)` pins `ProjectState.ChangeVersion` while materializing caller-provided lazy root element IDs, then reads project ownership from the same semantic revision. `SemanticHandleOwnershipResolver.Resolve(...)` currently performs `EnsureUniqueElementIds(project)` first, then materializes caller-provided lazy `selectedHandles` without checking whether that enumeration changed the project before ownership scanning continues.

A side-effecting lazy handle enumerable can therefore establish ownership resolution against a different project revision than the one whose element identity integrity was initially validated. This is inconsistent with the existing Locate/root-selection freshness contract and other lazy-input mutation boundaries in Core.

## Intended scope

- pin `ProjectState.ChangeVersion` across `selectedHandles` materialization in `SemanticHandleOwnershipResolver.Resolve(...)`;
- reject a changed project before semantic ownership scanning continues;
- preserve existing selected-handle normalization/deduplication/count bound, ownership conflict diagnostics, canonical stored-handle checks, ordering and empty-input behavior;
- add focused Core smoke coverage for stable lazy input and project-mutating lazy input.

## Reserved surfaces

- `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
- `tests/QS3D.Core.SmokeTests/SemanticHandleSelectionFreshnessSmoke.cs`
- this claim file

## Excluded scope

Do not modify generated-handle health/ownership policies, `SourceHandleResolver`, CAD selection/UI adapters, semantic untrack mutation behavior, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual execution.
