# Work claim — Auto Room source-handle resource bound

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-auto-room-source-handle-resource-bound-20260812-1525`
- Registered: `2026-08-12T15:25:00+07:00`
- Completed: `2026-08-12T15:30:41+07:00`
- Baseline main SHA: `da81d86bdff14edd5c7e86e520fdde1435a7215d`
- Claim commit: `a7f01e566b8304dc1f783c279953bc298b5fa1d4`
- Source fix: `82ae1c24e521f86c288a72c32e6b8e12f53e9e41`
- Regression smoke: `84f098dca13ad2bc7fab0c03a12c98cf2c8b9895`
- Smoke registration: `f5291f0be8d670f18d2929ac6752dae9b5effaa7`
- Priority: P1 resource-bound Core normalization

## Confirmed defect

`AutoRoomLifecycle.NormalizeSourceHandles(IEnumerable<string>)` applied an unbounded LINQ pipeline over caller-controlled input. A very large or non-terminating enumerable could therefore be consumed without the 5,000-handle fail-closed ceiling already enforced by `SourceHandleResolver` for persisted Auto Room boundary handles. The shared normalizer is public Core surface and is also used to canonicalize Auto Room source signatures, so the effective resource-bound contract was inconsistent.

## Completed scope

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs` now materializes source handles through an explicit bounded loop and rejects input entry 5,001 before processing it.
- `tests/QS3D.Core.SmokeTests/AutoRoomSourceHandleResourceBoundSmoke.cs` covers the lazy over-limit boundary, exact-limit acceptance, and canonical normalization compatibility.
- `tests/QS3D.Core.SmokeTests/AutoRoomSourceHandleResourceBoundRegistration.cs` auto-registers the focused smoke through `ModuleInitializer`.
- this claim file.

## Exclusions preserved

- No Project Browser, Recognition/Release #37, or SourceHandleResolver changes.
- No persistence schema, BricsCAD/native/UI, room generation topology, family synchronization, stale-state, or quantity behavior changes.

## Result

- `NormalizeSourceHandles` accepts at most 5,000 raw input entries and fails closed as soon as entry 5,001 is observed.
- The guard does not request entry 5,002 from lazy input after the limit breach.
- Existing valid normalization remains unchanged: blank entries are ignored; retained handles are trimmed, upper-cased invariantly, deduplicated case-insensitively, sorted case-insensitively, and joined with `;`.
- Exactly 5,000 input entries remain valid.

## Validation evidence

- Source commit readback shows only the `MaxSourceHandleInputCount = 5000` constant plus the bounded `NormalizeSourceHandles` implementation changed in `AutoRoomLifecycle.cs`.
- Current-main source blob after readback: `5620acf747e77fd23b0eb6ca3bf7d54a61ed5487`.
- Current-main smoke blob after readback: `1af08fb3c11a4a57a530aae80ab4065cc6cc767a`.
- Current-main registration blob after readback: `4491933c2a0e3cfe7dd33fcb83777bc61008a8c5`.
- Ancestry compare from claim `a7f01e566b8304dc1f783c279953bc298b5fa1d4` to registration `f5291f0be8d670f18d2929ac6752dae9b5effaa7` is `ahead` with no divergence; concurrent commits in other reserved scopes are preserved.
- At registration readback, `main` pointed to `f5291f0be8d670f18d2929ac6752dae9b5effaa7`.
- GitHub reported no combined status checks and no workflow runs for the registration commit. No executable smoke/full build/licensed BricsCAD runtime PASS is claimed for this lane.
