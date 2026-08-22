# Work claim — Generated stale state token canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-generated-stale-state-canonicality`
- Registered: `2026-08-12T10:48:00+07:00`
- Completed: `2026-08-12T10:50:00+07:00`
- Baseline main SHA: `b9972422699fd76c6f5ca912d72a0243925f70d2`
- Pull Request: `#781`
- Reviewed head: `d7a0f0e675081b040c690b55c6db6ffe76d242cc`
- Merge SHA: `82b9865506a30604f7746b33ac818cf95bd23100`
- Priority: P1 — padded persisted stale state can bypass both malformed-state and genuine-stale health reporting
- Task Key: `CORE-GENERATED-STALE-STATE-CANONICALITY`

## Confirmed defect

`ProjectElement` writers store each generated-output state as exact lowercase `"stale"`. `GeneratedGeometryStaleHealthService.InspectMalformedStaleMetadata(...)` trimmed state before recognizing stale metadata, while `ProjectElement.IsGenerated*Stale()` compares the state without trimming. Therefore a persisted state such as `" stale "` with a valid stale snapshot passed malformed-metadata validation but failed the genuine-stale query, allowing no stale-related issue.

## Completed implementation

- A per-output stale-state token that becomes `"stale"` only after trimming outer whitespace now emits Error `GENERATED_STALE_STATE_NON_CANONICAL`.
- Existing `GENERATED_STALE_METADATA_INVALID` missing-snapshot behavior remains unchanged.
- `ProjectElement` mutation/query semantics were not modified; inspection remains read-only.
- Exact lowercase `"stale"` with a matching snapshot continues through existing genuine-stale warning behavior.
- This lane did not add a casing rule for unpadded case variants.

## Regression evidence

`tests/QS3D.Core.SmokeTests/GeneratedStaleStateCanonicalitySmoke.cs` covers padded stale state with valid snapshot, exact stale state with matching generated solid, and padded stale state with missing snapshot.

PR #781 exact diff was reviewed as two files only (100 additions, 2 deletions). Guarded squash merge succeeded as `82b9865506a30604f7746b33ac818cf95bd23100`. Merged-main readback confirms source blob `78a3c2733824e5b1834dcade67f54a3b004f891b` and smoke blob `a730d7f8d397591d6a9e8ab278ca1d488fea4b3e`. Comparison from the merge SHA to moving `main` reported `behind_by=0` with merge base equal to the merge SHA; concurrent files were unrelated.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Satisfied: padded generated stale state can no longer false-clean health inspection, focused regression evidence is merged to current `main`, and this claim is closed `COMPLETED` with exact integration evidence.
