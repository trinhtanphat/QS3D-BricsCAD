# Work claim — opening host bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-opening-host-bounded-enumeration-20260811-2329`
- Registered: `2026-08-11T23:29:00+07:00`
- Baseline main SHA: `0fa9b09c0815e58a63d0102c9e5cf0ead2a0184e`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `OpeningHostMatcher.Match` enforce its existing `MaxSegments` contract while enumerating the source, instead of materializing an arbitrarily large enumerable before checking the cap.

## Expected surfaces

- `src/QS3D.Core/Geometry/OpeningHostMatcher.cs`
- `tests/QS3D.Core.SmokeTests/OpeningHostMatcherSmoke.cs`
- this claim file for close-out

## Concrete defect

`OpeningHostMatcher.Match` currently calls `source.ToList()` and only then checks `segments.Count > MaxSegments`. The declared 20,000-segment safety bound therefore does not bound enumeration or allocation: a huge or non-terminating enumerable can be consumed without limit before the guard executes.

## Explicit exclusions

- No host matching ranking/tolerance semantics changes.
- No auto-host command, native BricsCAD, Level, opening-cut, regeneration, UI, updater/licensing, interchange, Actions, release, or LOCAL_PASS work.
- No speculative host dependency or workflow expansion.

## Validation plan

- Preserve all existing host matching smoke behavior.
- Add a focused enumerable that throws if the matcher reads past `MaxSegments + 1`; verify the matcher rejects oversize input without consuming further values.
- Re-fetch current target blobs immediately before implementation writes and do not overwrite concurrent edits.
- No local `dotnet` or BricsCAD runtime is available in this environment; no GitHub Actions will be dispatched.

## Completion condition

The host matcher enforces its existing segment cap during enumeration, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact commit SHA(s) and validation actually performed.
