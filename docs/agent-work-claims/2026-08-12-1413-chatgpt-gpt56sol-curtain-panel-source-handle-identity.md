# Work claim — Curtain Panel SourceHandles numeric identity

- Status: `ACTIVE`
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

Shared generated ownership treats valid positive hexadecimal aliases such as `A` and `0A` as one CAD identity. PR #918 normalized Curtain Panel generated-handle duplicate/ownership/live checks but intentionally left `SourceHandles` comparison as raw trimmed spelling. Current code therefore misses a generated/source collision when generated metadata says `A` and `SourceHandles` says `0A`. Slab Mesh and other hardened providers already normalize SourceHandles before this overlap check; Semantic Tag PR #925 fixed the same class of defect.

## Validation plan

- Normalize each SourceHandle with `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` and compare to the already-computed generated `identity`.
- Extend existing auto-registered Curtain Panel numeric-handle smoke with alias-overlap rejection and a genuinely distinct source-handle control.
- Preserve project immutability assertions and all existing numeric live/duplicate/missing cases.
- Read back exact diffs; do not claim executable full smoke/build/Actions/V25 runtime PASS unless actually run.

## Completion condition

A pushed `main` implementation fixes source/generated alias overlap and adds focused regression coverage, followed by this claim marked `COMPLETED` with exact implementation SHA(s).
