# Work claim — Regeneration preview subset input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-preview-subset-freshness`
- Registered: `2026-08-12T00:36:00+07:00`
- Baseline main SHA: `f209d97920b20e4463aebb6c853562065e06ec14`
- Priority: P1 — bind subset-preview freshness before caller-controlled target enumeration.

## Confirmed defect

`RegenerationPreviewService.PreviewSubset(...)` currently enumerates the caller-provided `IEnumerable<string>` through `CanonicalPreviewTargets(elementIds, project.Elements.Count)` before `PreviewInternal(...)` captures `project.ChangeVersion`. A lazy target sequence can therefore mutate/touch the project during enumeration; the resulting preview is then stamped with the post-enumeration revision and the operation misses that the project changed while target scope was being established. The cardinality bound also reads live `project.Elements.Count` before this freshness window.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationPreviewService.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationPreviewSmoke.cs`
- `scripts/preflight-regeneration-preview-subset-freshness.py` (new)
- this claim file for close-out

## Intended contract

- `PreviewSubset(...)` captures `ChangeVersion` and semantic element cardinality before caller target enumeration.
- The canonical target bound uses the captured cardinality.
- Preview construction is stamped with the pre-enumeration revision; if enumeration changed the project, the subset preview fails closed instead of rebasing freshness after the change.
- Full-project preview behavior, canonical target validation, detached regeneration and apply guards remain unchanged.
- Focused smoke uses a lazy target source that touches the project during enumeration and proves stale rejection.

## Excluded scope

No RegenerationEngine/DependencyGraph rewrite, no native/UI work, no Actions dispatch, and no BricsCAD V25 runtime claim.

## Completion condition

Subset preview freshness begins before caller-controlled enumeration, focused regression/static coverage is on current `main`, and this claim is closed with exact SHAs and truthful validation boundaries.
