# Work claim — Documentation catalog required root sections

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:20:00+07:00`
- Baseline main SHA observed: `c79e8f1588aa0b28475ba7e8b604616cee530880`
- Priority: P1 — fail-closed documentation persistence completeness

## Confirmed defect

The v1 documentation catalog serializer always emits exactly one `<views>` section and exactly one `<sheets>` section, but `ValidateSchema(...)` only enforces *at most one* of each. If either root section is removed from stored metadata, `Load(...)` treats the missing container as an empty collection and can silently accept a lossy/corrupted catalog instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs` root-section cardinality only
- focused Core smoke regression
- `docs/plans/2026-08-12-documentation-catalog-required-root-sections.md`
- this claim file

## Contract

1. A format-v1 catalog must contain exactly one `<views>` root child and exactly one `<sheets>` root child.
2. Existing duplicate-section rejection remains.
3. Empty but present `<views />` / `<sheets />` remain valid.
4. Optional nested containers (`categories`, `include`, `exclude`, `placements`) keep their existing semantics.
5. No format-version, enum-token, save-bound, planner/editor/native behavior changes.

## Non-overlap

- Do not alter nested-container cardinality, documentation planners/editor/UI/native CAD, licensing, regeneration, XLSX/BOM/interchange/health lanes.
- No GitHub Actions dispatch or release publication.

## Closure

Claim first, planning before source, exact store blob re-fetch, regression from a real serialized payload with either root section removed, ancestry verification on moving `main`, truthful close without unexecuted CI/runtime PASS claims.
