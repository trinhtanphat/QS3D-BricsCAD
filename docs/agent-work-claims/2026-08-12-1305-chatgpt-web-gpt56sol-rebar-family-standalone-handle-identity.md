# Work claim — Rebar-family standalone numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-rebar-family-standalone-handle-identity`
- Registered: `2026-08-12T13:05:00+07:00`
- Baseline main SHA: `6d3eb76a2c08216a58284db524338e5172de2c1e`
- Priority: P0 — standalone rebar-family health providers must use the same numeric handle identity as shared ownership.
- Task Key: `CORE-REBAR-FAMILY-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

The shared ownership policy now canonicalizes valid positive CAD-hex identities, so `A` and `0A` refer to the same ownership identity. Six standalone rebar-family health providers still hash/compare provider-valid handles by trimmed text: `GeneratedRebarHealthService`, `GeneratedBeamStirrupHealthService`, `GeneratedTieRebarHealthService`, `GeneratedSlabMeshHealthService`, `GeneratedWallMeshHealthService`, and `GeneratedFoundationMeshHealthService`.

Because both `A` and `0A` pass the providers' existing hexadecimal validity check, a persisted handle list can contain both aliases of the same CAD object without the provider reporting a duplicate; local ownership/source/live checks can also use the wrong textual identity. This diverges from BricsCAD numeric Handle semantics and from the completed shared ownership fix.

## Non-overlap check

There were no open PRs at `main@6d3eb76a2c08216a58284db524338e5172de2c1e`, and the latest history search returned no standalone-health numeric handle identity lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- one focused Core smoke regression across the six providers
- this claim file

Do not change `0x` validity semantics, handle persistence spelling, existing dedicated handle-canonicality diagnostics, metadata numeric/mode logic, builders/planners, command wrappers, or BricsCAD runtime code.

## Intended contract

- Once a provider handle token has passed its existing hexadecimal validity rule, duplicate/local ownership/source/live checks use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`.
- `A` and `0A` are one logical CAD object for standalone provider duplicate and conflict checks.
- Existing invalid-token behavior remains unchanged, including current treatment of optional `0x` text in these providers.
- Existing whitespace canonicality diagnostics remain unchanged.
- Count checks use the count of unique valid numeric identities, preventing alias inflation.

## Completion condition

All six providers fail visible for numeric aliases of the same valid CAD handle while preserving existing invalid/canonical/metadata semantics; focused smoke coverage pins representative duplicate and ownership/source controls across providers; source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
