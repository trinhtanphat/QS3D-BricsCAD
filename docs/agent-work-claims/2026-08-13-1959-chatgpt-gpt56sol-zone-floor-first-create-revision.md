# Work claim — first Zone/Floor create revision canonicality

- Status: `BLOCKED_TEST_WRITE`
- Agent: `chatgpt-gpt56sol-zone-floor-first-create-revision-20260813`
- Registered: `2026-08-13T19:59:00+07:00`
- Source fixed: `2026-08-13T20:09:00+07:00`
- Baseline main SHA: `af9e216176691ffd0d8f6942489ab50f347bf24d`
- Priority: P0 deterministic persisted mutation revision semantics.

## Confirmed defect

`ProjectZoneService.Create()` and `ProjectFloorService.Create()` each called `project.Touch()` before adding the new definition, then auto-activated the first created item by assigning `ActiveZoneId` / `ActiveFloorId`. Those persisted scalar setters call `SetPersistedScalar()`, which increments `ChangeVersion` again. A first logical create therefore advanced the project revision twice while subsequent creates advanced once.

## Source fix landed

- Zone fix: `ec0617d6a350315b8891bc175e54c863149b3e15`; current source blob `77352af7ce79c04e7e0f3d691abfc06506fd98a9`.
- Floor fix: `ff09347e5b6400587112f68039b12cfa8c0187fa`; current source blob `3e5436f3bb3b5b08c47a4174aeaaa25ec658cc4e`.
- Both Create paths now choose exactly one revision-owning operation: when no active item exists they assign the canonical active id through its persisted scalar setter; otherwise they call `project.Touch()`. The definition is then added. First and subsequent successful creates therefore have one revision owner instead of two.
- First-created Zone/Floor auto-activation remains unchanged; general `ProjectState` persisted-scalar semantics were not modified.

## Regression status

A focused regression file `tests/QS3D.Core.SmokeTests/ProjectZoneFloorCreateRevisionSmoke.cs` was reserved to assert first create +1, subsequent create +1, canonical first active id, and preservation of the active id on second create.

The platform safety gate blocked every attempted executable regression write before mutation:

1. full replacement of the existing `ProjectZoneServiceSmoke.cs` was blocked before write;
2. `create_file` for the focused smoke was blocked before write;
3. Git Data `create_blob` for the same focused smoke was also blocked before write.

No test commit exists and no PASS is claimed. The claim is therefore not marked `COMPLETED`.

## Coordination / moving-main reconciliation

- Exact commit searches for `zone create ChangeVersion`, `zone double Touch`, `floor create ChangeVersion`, and `first floor active revision` returned no competing lane before claim.
- Claim commit: `7479b252ee36dbf5fc1e0ee4ea79a69ad4f92316`.
- Scope amendment commit: `4d1e501a66a81212e79d756455ec082ce1157adc`.
- Concurrent `ab9f1022ede0ff03b3d0ebafd7bedc41c83a35f4` touched only `scripts/preflight-runtime-product-version-identity.py`; it is disjoint from the reserved Domain files.
- Exact post-write readback confirms both current Create implementations still contain the mutually exclusive active-setter/Touch paths.

## Validation actually performed

- source/history/collision review: PASS;
- exact remote source readback: PASS;
- managed compile/smoke: `NOT_RUN` (hosted environment has no usable managed toolchain in this workstream);
- regression source landing: `BLOCKED_BY_PLATFORM_SAFETY_GATE`;
- GitHub Actions/native BricsCAD/package/release qualification: `NOT_RUN`.

## Remaining completion condition

Land the reserved focused regression (or equivalent assertions in the existing Zone/Floor service smokes) when executable test-file writes are available, then run/read back the managed smoke on an environment with the toolchain. Until then the production source correction is present on `main`, but this claim remains explicitly blocked rather than falsely completed.