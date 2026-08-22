# Work claim — Generated Tie Rebar handle token canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-tie-rebar-handle-canonicality`
- Registered: `2026-08-12T10:23:00+07:00`
- Last Updated: `2026-08-12T10:25:00+07:00`
- Completed: `2026-08-12T10:25:00+07:00`
- Baseline main SHA: `be17a4d1f2121e536a07df74e7239888c95a8f59`
- Priority: P1 — malformed persisted generated Tie Rebar owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-TIE-REBAR-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedTieRebarHealthService.Inspect(...)` split `GeneratedTieRebarHandles` with `StringSplitOptions.None` but immediately trimmed every token before validating it. A persisted token such as `" A "` therefore passed as valid hex and could complete ownership/live lookup with no canonicality issue. Current sibling generated-solid and generic generated-rebar diagnostics already enforce the writer-owned contract that surrounding token whitespace is fail-visible while downstream lookup continues using the trimmed handle; lowercase canonical hex remains accepted.

## Coordination

The prior Tie Rebar null-health claim is `COMPLETED`, and the empty-handle-token lane is also completed. No current commit/claim search found a Tie Rebar padded-handle canonicality lane before registration. Open PR #747 was Template Profile XML preflight and did not overlap this file.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedTieRebarHandleCanonicalitySmoke.cs`
- this claim file

## Implemented contract

- A valid non-empty Tie Rebar hex handle token with surrounding whitespace emits Error `TIE_REBAR_GENERATED_HANDLE_NON_CANONICAL`.
- Duplicate, ownership, SourceHandles, liveness, count, diameter, spacing, category and stale checks continue using the trimmed handle so canonicality does not create false missing/conflict results.
- Lower-case canonical hex remains accepted; no hex-letter casing rule was added.
- Empty/whitespace delimiter tokens continue to emit existing `INVALID_TIE_REBAR_GENERATED_HANDLE` diagnostics.
- Tie builders, generated ownership policy, CAD runtime code, persistence and quantity semantics were unchanged.

## Integration evidence

- Claim registration: `4aacc5cb406ba242b6f3efaff8071aa92daa5637`.
- Source branch commit: `b7f99003c35558996de3c46d7bee08f7ed4ff738`.
- Regression branch commit: `16622568a245aa89fc461a9048500f78646fb619`.
- PR: `#753` (`fix(health): surface padded tie rebar handles`).
- Squash merge on `main`: `24f065298648a1a10c3d73d939415c2a3c2990fa`.
- Merged source blob read back from `main`: `d53071352184f994b94aef5a56c85dde94e563ee`.
- Merged smoke blob read back from `main`: `47739b52dcc03769794803017ca3a1823da5a640`.
- Ancestry verification after merge: comparing `24f065298648a1a10c3d73d939415c2a3c2990fa` to moving `main` returned `ahead_by=1`, `behind_by=0`, with merge base equal to the squash SHA; the concurrent file change was unrelated `ProjectFloorService.cs`.

## Validation boundary

Source/test readback and merge ancestry were verified remotely. No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS was executed or claimed in this lane.

## Completion condition

Satisfied: padded generated Tie Rebar handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
