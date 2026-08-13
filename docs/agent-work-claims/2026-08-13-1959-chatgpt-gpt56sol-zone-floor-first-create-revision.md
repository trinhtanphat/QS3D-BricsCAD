# Work claim — first Zone/Floor create revision canonicality

- Status: `BLOCKED_STATIC_REGRESSION_WRITE`
- Agent: `chatgpt-gpt56sol-zone-floor-first-create-revision-20260813`
- Registered: `2026-08-13T19:59:00+07:00`
- Source fixed: `2026-08-13T20:09:00+07:00`
- Static regression resumed: `2026-08-13T21:18:00+07:00`
- Static regression write blocked: `2026-08-13T21:20:00+07:00`
- Baseline main SHA: `af9e216176691ffd0d8f6942489ab50f347bf24d`
- Static-regression baseline main SHA: `50ba32a6d0df94ae8433b643158a6fd67cdedfc6`
- Priority: P0 deterministic persisted mutation revision semantics.

## Confirmed defect

`ProjectZoneService.Create()` and `ProjectFloorService.Create()` each called `project.Touch()` before adding the new definition, then auto-activated the first created item by assigning `ActiveZoneId` / `ActiveFloorId`. Those persisted scalar setters call `SetPersistedScalar()`, which increments `ChangeVersion` again. A first logical create therefore advanced the project revision twice while subsequent creates advanced once.

## Source fix landed

- Zone fix: `ec0617d6a350315b8891bc175e54c863149b3e15`; current source blob `77352af7ce79c04e7e0f3d691abfc06506fd98a9`.
- Floor fix: `ff09347e5b6400587112f68039b12cfa8c0187fa`; current source blob `3e5436f3bb3b5b08c47a4174aeaaa25ec658cc4e`.
- Both Create paths now choose exactly one revision-owning operation: when no active item exists they assign the canonical active id through its persisted scalar setter; otherwise they call `project.Touch()`. The definition is then added. First and subsequent successful creates therefore have one revision owner instead of two.
- First-created Zone/Floor auto-activation remains unchanged; general `ProjectState` persisted-scalar semantics were not modified.

## Regression status

The originally reserved executable regression `tests/QS3D.Core.SmokeTests/ProjectZoneFloorCreateRevisionSmoke.cs` remains unwritten because the platform safety gate blocked executable test-file writes before mutation. No C# test commit exists and no managed/native PASS is claimed.

Repository convention review established the static fallback without guessing filenames:

- `scripts/preflight-zones.py` is the Zone feature-presence gate;
- `scripts/preflight-project-floor-zone-mutation-integrity.py` is the existing combined Floor/Zone domain mutation-integrity gate and already reads both `ProjectFloorService.cs` and `ProjectZoneService.cs`;
- no separate `preflight-floor*` gate exists in repository search.

The reserved regression target was therefore amended to `scripts/preflight-project-floor-zone-mutation-integrity.py`. A bounded replacement was prepared to isolate both `Create()` methods, require the first-create active-id setter and subsequent-create `project.Touch()` as mutually exclusive revision owners, require collection insertion after that branch, and guard the persisted active-id setter contract in `ProjectState.cs`.

The connector/platform safety gate blocked the `update_file` attempt for that executable Python preflight before repository mutation. No regression commit resulted. This lane does not retry through `create_blob`, Git Data, force-ref movement, or another write mechanism because that would bypass the safety gate rather than resolve it.

## Coordination / moving-main reconciliation

- Exact commit searches for `zone create ChangeVersion`, `zone double Touch`, `floor create ChangeVersion`, and `first floor active revision` returned no competing lane before claim.
- Claim commit: `7479b252ee36dbf5fc1e0ee4ea79a69ad4f92316`.
- Scope amendment commit: `4d1e501a66a81212e79d756455ec082ce1157adc`.
- Blocked-regression record commit: `b7a30eefe5bddd604e66d3695f0b31b0219f5780`.
- Static-regression resume/amendment commit: `913d5f0c74c6e6f9283c0629d1ea3431d5061f7c`.
- Concurrent `ab9f1022ede0ff03b3d0ebafd7bedc41c83a35f4` touched only `scripts/preflight-runtime-product-version-identity.py`; it is disjoint from the reserved Domain files.
- Moving `main` reached `50ba32a6d0df94ae8433b643158a6fd67cdedfc6` before the claim amendment; that commit touches only `src/QS3D.Core/Revisions/RevisionMath.cs` and is disjoint from this lane.
- Branch search for `zone-floor` returned no competing branch immediately before the static-regression amendment.
- Exact remote source readback confirms both current Create implementations still contain the mutually exclusive active-setter/Touch paths.

## Validation actually performed

- source/history/collision review: PASS;
- exact remote Zone/Floor source readback: PASS;
- exact `ProjectState` active-id persisted-scalar readback: PASS;
- static gate discovery/readback: PASS;
- static regression source landing: `BLOCKED_BY_PLATFORM_SAFETY_GATE`;
- managed compile/smoke: `NOT_RUN`;
- GitHub Actions/native BricsCAD/package/release qualification: `NOT_RUN`.

## Remaining completion condition

When executable static-test writes are available without bypassing the safety gate, land the bounded regression in `scripts/preflight-project-floor-zone-mutation-integrity.py`, exact-readback it, and validate the source-level conditions against the current Zone/Floor/ProjectState blobs. Until then this claim remains blocked rather than falsely completed. Do not claim BricsCAD V25 `LOCAL_PASS` and do not dispatch GitHub Actions from this lane.