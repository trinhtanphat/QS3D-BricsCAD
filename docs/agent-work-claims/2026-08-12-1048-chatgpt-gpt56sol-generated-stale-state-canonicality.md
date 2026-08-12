# Work claim — Generated stale state token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-generated-stale-state-canonicality`
- Registered: `2026-08-12T10:48:00+07:00`
- Last Updated: `2026-08-12T10:48:00+07:00`
- Baseline main SHA: `b9972422699fd76c6f5ca912d72a0243925f70d2`
- Priority: P1 — padded persisted stale state can bypass both malformed-state and genuine-stale health reporting
- Task Key: `CORE-GENERATED-STALE-STATE-CANONICALITY`

## Confirmed defect

`ProjectElement` writers store each generated-output state as exact lowercase `"stale"`. `GeneratedGeometryStaleHealthService.InspectMalformedStaleMetadata(...)` currently trims state before recognizing stale metadata, while `ProjectElement.IsGenerated*Stale()` compares the state without trimming. Therefore a persisted state such as `" stale "` with a valid stale snapshot is accepted by the malformed-metadata preflight (snapshot exists) but rejected by the stale query, allowing the element to return no stale-related issue at all.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedStaleStateCanonicalitySmoke.cs`
- this claim file

## Intended contract

- If a per-output stale-state token becomes `"stale"` only after trimming outer whitespace, emit Error `GENERATED_STALE_STATE_NON_CANONICAL`.
- Keep existing missing-snapshot Error behavior unchanged.
- Keep `ProjectElement` mutation/query semantics unchanged and inspection read-only.
- Exact lowercase `"stale"` with a matching snapshot continues through existing genuine-stale warning behavior.
- Existing case-insensitive stale recognition without surrounding whitespace is not broadened into a new casing rule in this lane.

## Validation plan

Add an auto-registered Core smoke proving padded stale state + valid snapshot is fail-visible, exact stale state with matching generated solid remains a genuine stale warning without canonicality Error, and padded stale state with missing snapshot reports both canonicality and existing malformed metadata Error. Review exact PR diff, merge guarded, read back source/test and verify ancestry.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated stale state can no longer false-clean health inspection, focused regression evidence is merged to current `main`, and this claim is closed with exact integration evidence.
