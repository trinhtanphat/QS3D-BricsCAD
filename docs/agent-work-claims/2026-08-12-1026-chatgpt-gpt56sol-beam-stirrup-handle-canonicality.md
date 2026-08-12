# Work claim — Generated Beam Stirrup handle token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-beam-stirrup-handle-canonicality`
- Registered: `2026-08-12T10:26:00+07:00`
- Last Updated: `2026-08-12T10:26:00+07:00`
- Baseline main SHA: `18d29069348f0808b3b3a24ae7236c08d63c1a9b`
- Priority: P1 — malformed persisted generated Beam Stirrup owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-BEAM-STIRRUP-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedBeamStirrupHealthService.Inspect(...)` preserves empty delimiter tokens with `StringSplitOptions.None` but trims each token before validating it. A persisted value such as `" A "` therefore passes as a valid hex handle with no canonicality issue. Sibling generated-solid, generic generated-rebar and Tie Rebar diagnostics now enforce the writer-owned contract that surrounding token whitespace is fail-visible while ownership/live lookup continues using the trimmed handle; lowercase canonical hex remains valid.

## Coordination

Beam Stirrup null-health, empty-handle-token and length-overflow lanes are completed. No current commit/claim search found a Beam Stirrup padded-handle canonicality lane. Open Template Profile/local fixture work does not overlap this diagnostic file.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedBeamStirrupHandleCanonicalitySmoke.cs`
- this claim file

## Intended contract

- A valid non-empty Beam Stirrup hex token with surrounding whitespace emits Error `BEAM_STIRRUP_GENERATED_HANDLE_NON_CANONICAL`.
- Continue duplicate, ownership, SourceHandles, liveness, count, diameter, advanced metadata, category and stale checks using the trimmed handle.
- Lower-case canonical hex remains accepted; no casing rule is added.
- Empty/whitespace delimiter tokens continue to emit existing `INVALID_BEAM_STIRRUP_GENERATED_HANDLE` diagnostics.
- Do not modify beam-stirrup builders, advanced metadata semantics, overflow policy, generated ownership policy, CAD runtime code or persistence.

## Validation plan

Add an auto-registered Core smoke covering padded handle + trimmed live lookup, lowercase canonical control, and preservation of the completed empty-token invalid diagnostic. Review exact PR diff, squash-merge guarded, read back source/test and verify ancestry.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated Beam Stirrup handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
