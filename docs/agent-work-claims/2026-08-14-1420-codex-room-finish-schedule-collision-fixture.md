# Work claim — Room Finish schedule collision fixture canonical IDs

- Status: `COMPLETED`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T14:20:40+07:00`
- Completed: `2026-08-14T14:23:28+07:00`
- Baseline main SHA: `bf8ae801b5b59fd415bf5b561b087cf906dbfe95`
- Priority: first independent Core smoke blocker after the revision-capture fixture correction

## Reserved scope

Reconcile the completed Room Finish group-key regression with the canonical ID contract by replacing the control-character separator with valid printable `|`, explicitly proving that the two distinct six-token tuples still serialize identically under the historical delimiter-only key, and preserving the existing three-row split, aggregation, quantity and provenance assertions.

Expected test-only surfaces:

- `tests/QS3D.Core.SmokeTests/RoomFinishScheduleGroupKeyCollisionSmoke.cs`
- `scripts/preflight-room-finish-group-key-collision.py`, only to update its stale fixture token lock and keep the focused source/coverage contract executable

## Contract

- `FloorDefinition` and `ProjectElement` IDs canonically reject control characters; production must remain unchanged.
- The historical six-token grouping order remains floor ID, stable room ID/unlinked sentinel, category, family ID, material and unit hint.
- The current length-prefixed production key must keep distinct accepted tuples separate, while identical unlinked tuples continue aggregating.

## Excluded scope

- No production/domain/reporting change.
- No Level, probe, runner, BricsCAD adapter/runtime, private drawing, packaging, release, or GitHub Actions work.
- Do not weaken or remove any existing row-count, quantity, element, room or source provenance assertion.

## Validation plan

- Build `QS3D.Core.SmokeTests` in Release.
- Run `scripts/preflight-room-finish-group-key-collision.py`.
- Run the full registered Core smoke executable and report the next first independent blocker if any.
- Read back the merged test/gate from current `main` and close this claim with exact SHAs.

## Completion condition

A normal merged PR changes only the two reserved test surfaces, preserves the real collision and aggregation semantics with valid IDs, records focused/full-smoke evidence, and closes this claim on `main`.

## Completion record

- Claim-only merge: `d10f7ab04c8c53a1721ab2fbbe7274f03135f9f5` via PR #1187.
- Implementation merge: `ea11d8638d6bdef2a2d2b1df32d676abe0f27881` via PR #1188.
- Readback from current `main` confirms the fixture uses accepted printable `|` IDs, explicitly asserts equality of the two historical delimiter-only keys across all six production grouping tokens, and retains the three-row split plus all existing aggregation, quantity, element, room and source provenance assertions.
- The focused preflight now locks the valid printable fixture and explicit local serializer proof while retaining every production length-prefix/group-token-order guard.
- `dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release --no-restore`: PASS, 0 warnings / 0 errors.
- `preflight-room-finish-group-key-collision.py`: PASS.
- Full registered Core smoke: PASS (`ALL PASS`).
- No production, Level, probe, runner, BricsCAD adapter/runtime, private data or GitHub Actions surface was changed or exercised.
