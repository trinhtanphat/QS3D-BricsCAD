# Work claim — Curtain Panel SourceHandles numeric identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T14:13:20+07:00`
- Baseline main SHA: `f54e49495a11471f8374dcafd60f639903f0f3ef`
- Priority: `P0 Core health integrity — generated Curtain Panel vs SourceHandles overlap still compares raw hex spelling after the rest of the provider adopted shared numeric CAD-handle identity`

## Reserved scope

Make `GeneratedCurtainPanelHealthService` detect generated/source overlap using the same shared numeric CAD-handle identity already used for Curtain Panel duplicate, ownership, and live-handle checks. Add focused regression coverage to the existing numeric-handle smoke.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelNumericHandleIdentitySmoke.cs`

## Excluded scope

- Curtain Panel writer/runtime/materialization/probe behavior.
- Handle validity policy, persisted spelling, whitespace diagnostics, duplicate/count semantics, or live-handle semantics already implemented by PR #918.
- Slab/Wall/Rebar/Grid/Semantic Tag providers, DependencyGraph, or any other active lane.
- GitHub Actions dispatch, BricsCAD runtime qualification, packaging/release.

## Evidence

Shared generated ownership treats valid positive hexadecimal aliases such as `A` and `0A` as one CAD identity. PR #918 normalized Curtain Panel generated-handle duplicate/ownership/live checks but intentionally left `SourceHandles` comparison as raw trimmed spelling. Current code therefore missed a generated/source collision when generated metadata said `A` and `SourceHandles` said `0A`. Slab Mesh and other hardened providers already normalize SourceHandles before this overlap check; Semantic Tag PR #925 fixed the same class of defect.

## Validation

- Product fix: `48f827ceed0e4f54e817a5e8457282d873c55179` (`fix(health): normalize Curtain Panel SourceHandles identity`). Readback confirms exactly one production line changed: SourceHandles now normalize through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` and compare against the already-normalized generated identity.
- Regression: `36d794a04341bf956b5b9b928184b9900541f3eb` (`test(health): cover Curtain Panel SourceHandles aliases`). The existing module-initialized numeric-handle smoke now requires `A` vs `0A` source/generated overlap to report `CURTAIN_PANEL_GENERATED_HANDLE_IN_SOURCE` and keeps a distinct `B` control clean for that diagnostic.
- Existing live-handle, duplicate-spelling, missing-live, project-version, ownership and count assertions remain intact.
- No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied by pushed product fix `48f827ceed0e4f54e817a5e8457282d873c55179`, regression `36d794a04341bf956b5b9b928184b9900541f3eb`, and this completion record on `main`.
