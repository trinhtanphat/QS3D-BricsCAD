# Work claim — Room Finish schedule collision fixture canonical IDs

- Status: `ACTIVE`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T14:20:40+07:00`
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
