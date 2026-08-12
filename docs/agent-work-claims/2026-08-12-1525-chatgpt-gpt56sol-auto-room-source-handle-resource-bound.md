# Work claim — Auto Room source-handle resource bound

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-auto-room-source-handle-resource-bound-20260812-1525`
- Registered: `2026-08-12T15:25:00+07:00`
- Baseline main SHA: `da81d86bdff14edd5c7e86e520fdde1435a7215d`
- Priority: P1 resource-bound Core normalization

## Confirmed defect

`AutoRoomLifecycle.NormalizeSourceHandles(IEnumerable<string>)` currently applies an unbounded LINQ pipeline over caller-controlled input. A very large or non-terminating enumerable can therefore be consumed without the 5,000-handle fail-closed ceiling already enforced by `SourceHandleResolver` for persisted Auto Room boundary handles. The shared normalizer is public Core surface and is also used to canonicalize Auto Room source signatures, so the effective resource-bound contract is inconsistent.

## Reserved scope

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs` — bound `NormalizeSourceHandles` input enumeration only.
- `tests/QS3D.Core.SmokeTests/AutoRoomSourceHandleResourceBoundSmoke.cs` — focused Core regression.
- `tests/QS3D.Core.SmokeTests/AutoRoomSourceHandleResourceBoundRegistration.cs` — smoke registration only.
- this claim file.

## Active-claim exclusions

- Do not modify Project Browser selection/root identity work currently reserved by another active claim.
- Do not modify Recognition/Release #37 work; that lane completed immediately before this claim.
- No SourceHandleResolver behavior changes; its existing 5,000 persisted-boundary ceiling is the parity reference only.
- No persistence schema, BricsCAD/native/UI, room generation topology, family synchronization, stale-state, or quantity behavior changes.

## Intended contract

- `NormalizeSourceHandles` accepts at most 5,000 raw input entries and fails closed as soon as entry 5,001 is observed.
- The guard must not enumerate past the first over-limit entry, including for lazy/non-collection input.
- Existing normalization for valid input remains unchanged: blank entries are ignored; retained handles are trimmed, upper-cased invariantly, deduplicated case-insensitively, sorted case-insensitively, and joined with `;`.
- Exactly 5,000 input entries remain valid.

## Validation plan

Focused ModuleInitializer smoke supplies a lazy over-limit enumerable that would throw if the normalizer reads beyond entry 5,001, verifies the resource-limit diagnostic, and verifies a 5,000-entry control plus canonical trim/case/dedup ordering. Source/test/registration will be read back from current `main`, ancestry will be verified, and the claim will be closed only after all substantive commits are present. No GitHub Actions or licensed BricsCAD runtime PASS is claimed by this lane unless such evidence is actually observed.
