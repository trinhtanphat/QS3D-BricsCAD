# Work claim — Foundation Mesh numeric handle identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T14:18:00+07:00`
- Baseline main SHA: `36ee7c1799df6edcc14b27746078234bd1917633`
- Priority: `P0 Core health integrity — Foundation Mesh remains on raw textual CAD-handle identity while sibling generated-output providers use the shared numeric identity contract`

## Reserved scope

Align `GeneratedFoundationMeshHealthService` generated-handle duplicate/count, ownership index, SourceHandles overlap and live-handle checks with `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`, preserving existing validity and whitespace-canonicality behavior. Extend the existing auto-registered Foundation Mesh handle smoke with numeric-alias regression coverage.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedFoundationMeshHandleCanonicalitySmoke.cs`

## Excluded scope

- Foundation mesh geometry/planning/writers/native materialization or LOCAL-only qualification.
- Numeric handle identity policy itself; no changes to accepted/rejected handle syntax.
- Slab/Wall/Rebar/Grid/Curtain/Semantic Tag providers, DependencyGraph, schedules, or other active lanes.
- GitHub Actions dispatch, BricsCAD runtime qualification, packaging/release.

## Evidence

`GeneratedHandleOwnershipPolicy` defines numeric CAD-handle identity and current Slab/Wall/Rebar/Grid/Curtain/Semantic Tag health paths have been hardened around it. Foundation Mesh still adds raw trimmed `handle` to its local set, indexes owners using trim-only `Reserve`, compares SourceHandles by raw trimmed text, and calls `liveSolidHandles.Contains(handle)`. Therefore aliases such as generated `A` vs `0A` can evade duplicate/source/ownership collision checks or produce false missing-live diagnostics. Existing PR #762 only addressed whitespace canonicality and explicitly preserved these older comparisons.

## Validation plan

- Normalize provider-valid generated handles once and use that identity for local uniqueness/count, ownership, SourceHandles, and live lookup.
- Normalize ownership-index reservation and use a logical live-handle helper.
- Preserve empty/invalid token and padded-token diagnostics, lowercase acceptance, metadata/domain checks and stale behavior.
- Add focused smoke cases for numeric-equivalent live handle, duplicate spellings, source overlap, and cross-owner alias conflict while retaining existing whitespace/empty controls.
- Read back exact diffs; do not claim executable build/full smoke/Actions/V25 runtime PASS unless actually run.

## Completion condition

Pushed `main` product and regression commits align Foundation Mesh with shared numeric handle identity, followed by this claim marked `COMPLETED` with exact SHAs.
