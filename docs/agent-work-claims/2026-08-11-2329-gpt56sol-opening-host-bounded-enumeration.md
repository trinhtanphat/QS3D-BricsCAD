# Work claim — opening host bounded enumeration

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-opening-host-bounded-enumeration-20260811-2329`
- Registered: `2026-08-11T23:29:00+07:00`
- Completed: `2026-08-11T23:32:00+07:00`
- Baseline main SHA: `0fa9b09c0815e58a63d0102c9e5cf0ead2a0184e`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `OpeningHostMatcher.Match` enforce its existing `MaxSegments` contract while enumerating the source, instead of materializing an arbitrarily large enumerable before checking the cap.

## Expected surfaces

- `src/QS3D.Core/Geometry/OpeningHostMatcher.cs`
- `tests/QS3D.Core.SmokeTests/OpeningHostMatcherSmoke.cs`
- this claim file for close-out

## Concrete defect

`OpeningHostMatcher.Match` called `source.ToList()` and only then checked `segments.Count > MaxSegments`. The declared 20,000-segment safety bound therefore did not bound enumeration or allocation: a huge or non-terminating enumerable could be consumed without limit before the guard executed.

## Explicit exclusions

- No host matching ranking/tolerance semantics changes.
- No auto-host command, native BricsCAD, Level, opening-cut, regeneration, UI, updater/licensing, interchange, Actions, release, or LOCAL_PASS work.
- No speculative host dependency or workflow expansion.

## Validation performed

- Re-read current remote source after implementation: source enumeration is capped with `Take(MaxSegments + 1)` before materialization, preserving the existing `> MaxSegments` rejection and all ranking/tolerance logic.
- Re-read the focused smoke after integration: `OversizeSourcesAreBounded` uses a non-terminating probe that throws if a caller asks for item 20,002; the matcher rejects after exactly 20,001 yielded segments.
- Existing host matching scenarios remain registered in the same smoke module.
- A concurrent `main` write caused the first close-out update to return HTTP 409; the claim was re-read and the close-out retried without force or overwrite.
- No local `dotnet` or BricsCAD runtime is available in this environment; no GitHub Actions were run or dispatched.

## Implementation commits

- `1256d2a3bdf7e7b6335afb5f7b674cf2892b8b17` — `fix(core): bound opening host source enumeration`
- `449187efd9e2e45d04e764d374d2afaa9baa3041` — `test(core): guard opening host enumeration cap`

## Result

The existing 20,000-segment host-matching safety limit now bounds source enumeration/allocation instead of being checked only after unrestricted materialization.
