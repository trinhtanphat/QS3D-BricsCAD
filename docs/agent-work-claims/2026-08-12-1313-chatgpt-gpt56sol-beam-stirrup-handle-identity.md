# Work claim — Beam Stirrup standalone numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-beam-stirrup-handle-identity-20260812-1313`
- Registered: `2026-08-12T13:13:00+07:00`
- Baseline main SHA: `b5daac1da22bee96fedb740c7b340b023ec96c9e`
- Priority: P0 generated ownership/health identity parity
- Task Key: `CORE-BEAM-STIRRUP-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

`GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` canonicalizes valid positive CAD hexadecimal identities, so `A` and `0A` represent the same logical CAD handle. `GeneratedBeamStirrupHealthService` still uses trimmed raw text for its local duplicate set, valid-handle count, ownership lookup, SourceHandles comparison and live-handle lookup. Therefore a persisted list such as `A;0A` can count one CAD object twice, evade the provider's duplicate diagnostic, and diverge from shared ownership semantics.

The broader rebar-family standalone-handle claim explicitly released `GeneratedBeamStirrupHealthService.cs` for a separate follow-up claim. Current open-PR/history checks found no Beam Stirrup standalone numeric-handle identity lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- `tests/QS3D.Core.SmokeTests/BeamStirrupHandleIdentitySmoke.cs`
- this claim file

## Intended contract

- Preserve the provider's existing hexadecimal validity rule and whitespace canonicality diagnostic.
- After a token passes the existing validity rule, normalize it through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` for local duplicate/count, ownership, SourceHandles and live-handle checks.
- Build the provider-local ownership index with the same normalized numeric identity so aliases cannot create separate ownership buckets.
- `A` and `0A` must be treated as one logical CAD object; valid-handle count must count unique normalized identities only.
- Preserve all existing Beam Stirrup count, diameter, spacing, mode, advanced numeric, stale and category behavior.
- Do not change persisted handle spelling, builders/native code, shared ownership policy or `0x` validity behavior.

## Validation plan

Add a focused auto-registered Core smoke proving numeric aliases produce the existing duplicate/count diagnostics rather than inflating valid count, and proving SourceHandles/live/ownership checks use normalized identity while canonical controls remain unchanged.

## Validation boundary

Connector-only source/readback + focused smoke source. No GitHub Actions, full .NET executable smoke, or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
