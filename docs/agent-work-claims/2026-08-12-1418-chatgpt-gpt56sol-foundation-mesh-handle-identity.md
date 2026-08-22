# Work claim — Foundation Mesh numeric handle identity

- Status: `COMPLETED`
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

`GeneratedHandleOwnershipPolicy` defines numeric CAD-handle identity and current Slab/Wall/Rebar/Grid/Curtain/Semantic Tag health paths have been hardened around it. Foundation Mesh was still adding raw trimmed `handle` to its local set, indexing owners using trim-only `Reserve`, comparing SourceHandles by raw trimmed text, and calling `liveSolidHandles.Contains(handle)`. Therefore aliases such as generated `A` vs `0A` could evade duplicate/source/ownership collision checks or produce false missing-live diagnostics. Existing PR #762 only addressed whitespace canonicality and explicitly preserved these older comparisons.

## Validation

- Product fix: `5505585e95afcaa67bb4c2e8b812deeb2fb9f7ac` (`fix(health): normalize Foundation Mesh handle identity`). Readback confirms the provider now normalizes valid generated handles for local uniqueness/count, ownership lookup, SourceHandles comparison and live lookup; ownership-index `Reserve` now uses the same shared identity.
- Regression: `db45d8e0ef383420e4075268de479310f1e6bfcc` (`test(health): cover Foundation Mesh numeric handles`). Existing auto-registered canonicality smoke now covers numeric-equivalent live handle acceptance, duplicate aliases, SourceHandles alias collision and cross-owner alias conflict, while retaining padded/lowercase/empty-token controls.
- Existing validity, whitespace-canonicality, count/domain, faces/mode/footprint/category and stale behavior were not deliberately changed.
- No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied by pushed product fix `5505585e95afcaa67bb4c2e8b812deeb2fb9f7ac`, regression `db45d8e0ef383420e4075268de479310f1e6bfcc`, and this completion record on `main`.
