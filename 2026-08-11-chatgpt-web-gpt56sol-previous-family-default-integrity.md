# Work claim — Previous Family default integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-previous-family-default-integrity`
- Registered: `2026-08-11T21:07:00+07:00`
- Completed: `2026-08-11T21:18:00+07:00`
- Baseline main SHA: `55357e57981a996b3b3bfa75f19f5969184d2af6`
- Reservation SHA: `dcd3158b03e8df91bf63d552ace7f98a291988a1`
- Implementation PR: `#459`
- Squash merge SHA: `4fa587278e84df0ef10bf560a9687dcdc81cbf7f`
- Priority: confirmed Core integrity defect — Family reassignment and Auto Room synchronization validated the target Family before mutation but still consumed the previous Family's raw property map directly. In assignment this occurred after `ProjectState.Touch()`, so malformed previous-Family defaults could participate in a committed semantic mutation instead of failing closed under the same structural contract already enforced for target/source Family snapshots.

## Reserved scope

Harden only previous-Family default consumption at the two Core transfer boundaries already using `ProjectFamilyService.SnapshotProperties(...)` for target data. Resolve and validate every previous Family property snapshot completely before any project/element/room/metadata mutation, then consume the immutable validated snapshots during mutation planning/application.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs`
- this claim file for close-out

## Excluded scope

- No new Family property schema and no numeric parsing policy; preserve the existing canonical key/value structural contract only.
- No Family catalog CRUD redesign, BulkEdit behavior beyond transitively inheriting the hardened `ProjectFamilyService.Assign(...)`, or Family UI/Workspace changes.
- No `RoomBoundaryCommands.cs` / native Room Auto regeneration work.
- No QSDB/`ProjectSession` persistence/session surfaces owned by `chatgpt-web-gpt56sol-core-atomicity-20260811-1930`.
- No Create Similar/Direct Draw, Ribbon, Quantity/BQ, updater/release/signing, LOCAL inbox edits, GitHub Actions dispatch, or BricsCAD V25 runtime qualification.

## Implemented contract

- `ProjectFamilyService.Assign(...)` now validates and caches an immutable property snapshot for each unique previous Family while assembling the complete pending assignment batch, before `ProjectState.Touch()`.
- Assignment cleanup consumes only the validated cached snapshot; raw previous-Family properties are no longer iterated after the persistence mutation boundary.
- `AutoRoomLifecycle.SyncFamilyDefaults(...)` validates the previous Room Family before mutation planning whenever the Family actually changes.
- Auto Room inherited-default comparison and stale-default cleanup now consume the validated previous-Family snapshot/map rather than the live raw property dictionary.
- Existing target-Family validation, successful inherited-default behavior and instance overrides remain intact; no numeric/property schema was added.

## Focused regression coverage authored

- `ProjectFamilyAssignmentAtomicitySmoke.MalformedPreviousFamilyBlocksWholeAssignmentBeforeMutation()` injects a non-canonical previous-Family key and asserts rejection before element `FamilyId`, properties, dirty state, element `UpdatedUtc`, project `ChangeVersion` or project `UpdatedUtc` changes.
- `AutoRoomLifecycleSmoke.MalformedPreviousFamilyDefaultsFailBeforeMutation()` injects a previous Room Family value over the existing 1000-character contract and asserts rejection before Room `FamilyId`, Room properties, AutoRoom metadata, dirty state, Room `UpdatedUtc`, project `ChangeVersion` or project `UpdatedUtc` changes.
- Existing successful Auto Room instance-override regression remains registered.

## Exact implementation record

- `308f202577ae8521f519c3d3ad6b12e0f2de1c3d` — validate/cache previous Families before assignment.
- `6fe17f490ed0f674d1f350d5ce7fb89cfdbf3385` — validate previous Auto Room Family defaults before planning.
- `7241e2870a85bd28414ade7aac506c1fffeff5f8` — assignment failure-atomicity regression.
- `4fb861b335c9746b3aa711cc1308ab44ef63d8b9` — Auto Room previous-Family regression.
- `410681f53fe868429b658d20ee1e35c739e33a65` — conflict-safe branch synchronization: latest reviewed main tree plus the four claimed blobs, with the previous branch head as a second parent; branch ref moved by fast-forward only, never force-pushed.
- PR `#459` squash-merged as `4fa587278e84df0ef10bf560a9687dcdc81cbf7f`.

## Validation actually performed

- Re-fetched current source and active claims before reservation and implementation.
- Verified the four main-side target blobs remained unchanged while concurrent agents landed command/UI/reporting work.
- Reviewed the branch compare: exactly four claimed source/test files, with no unrelated repository surface.
- Before branch synchronization, explicitly re-fetched all four current-main blob SHAs and confirmed they still matched the pre-implementation baseline blobs.
- Built a latest-main-derived tree overlaying only the four already-reviewed branch blobs, then advanced the branch with a non-force merge commit so no concurrent main work was discarded.
- GitHub accepted the squash merge of PR `#459` as `4fa587278e84df0ef10bf560a9687dcdc81cbf7f`.
- No GitHub Actions/release was dispatched.
- No BricsCAD V25/native runtime PASS is claimed.
- The Core smoke regressions are committed deterministic coverage, but this connector-only lane did not execute the smoke binary locally and does not report it as runtime PASS.

## Coordination

The Core mutation-atomicity claim remained focused on QSDB/`ProjectSession` persistence/session surfaces. Create Similar remained on command/Ribbon/local-handoff surfaces. Room Auto concurrent work remained command-side and did not own these Core Family-default transfer boundaries. No competing claim was overwritten.

## Completion condition

`COMPLETED`: both Family reassignment and Auto Room Family switching now reject malformed previous-Family defaults before canonical mutation, focused regressions lock state preservation on failure, PR `#459` is merged to `main`, and exact source-review/merge evidence is recorded above.