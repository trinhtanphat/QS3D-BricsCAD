# Work claim — Rebar standalone numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-rebar-family-standalone-handle-identity`
- Registered: `2026-08-12T13:05:00+07:00`
- Scope refined: `2026-08-12T13:10:00+07:00`
- Baseline main SHA: `6d3eb76a2c08216a58284db524338e5172de2c1e`
- Priority: P0 — standalone longitudinal/shape and Tie Rebar health must use the same numeric handle identity as shared ownership.
- Task Key: `CORE-REBAR-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

The shared ownership policy now canonicalizes valid positive CAD-hex identities, so `A` and `0A` refer to the same ownership identity. `GeneratedRebarHealthService` and `GeneratedTieRebarHealthService` still hashed/compared provider-valid handles by trimmed text. Because both `A` and `0A` pass the providers' existing hexadecimal validity check, a persisted list could contain aliases of the same CAD object without a duplicate issue, inflate the valid-handle count, and use the wrong identity for local ownership/source/live checks.

The same pattern was confirmed in Beam Stirrup and mesh providers during audit, but those files are released from this atomic lane and will be handled under separate claims. This keeps each hot provider patch independently reviewable and avoids replacing large concurrently moving files in one PR.

## Non-overlap check

There were no open PRs at the original baseline and the latest history search returned no standalone-health numeric handle identity lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- one focused Core smoke regression across these two providers
- this claim file

Released for separate follow-up claims:
- `GeneratedBeamStirrupHealthService.cs`
- `GeneratedSlabMeshHealthService.cs`
- `GeneratedWallMeshHealthService.cs`
- `GeneratedFoundationMeshHealthService.cs`

Do not change `0x` validity semantics, handle persistence spelling, existing dedicated handle-canonicality diagnostics, metadata numeric/mode logic, builders/planners, command wrappers, or BricsCAD runtime code.

## Intended contract

- Once a provider handle token has passed its existing hexadecimal validity rule, duplicate/local ownership/source/live checks use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`.
- `A` and `0A` are one logical CAD object for standalone duplicate and conflict checks.
- Existing invalid-token behavior remains unchanged, including current treatment of optional `0x` text in these providers.
- Existing whitespace canonicality diagnostics remain unchanged.
- Count checks use the count of unique valid numeric identities, preventing alias inflation.

## Completion condition

Generic/Shape Rebar and Tie Rebar fail visible for numeric aliases of the same valid CAD handle while preserving existing invalid/canonical/metadata semantics; focused smoke coverage pins duplicate, count, source and ownership controls; source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
