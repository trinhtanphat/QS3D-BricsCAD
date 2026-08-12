# Work claim — Beam longitudinal layout snapshot ownership

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:16:00+07:00`
- Completed: `2026-08-12T00:18:00+07:00`
- Baseline main SHA: `fa73d76c8d76de5c53ebaa458a492d4b1716f0f0`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`BeamLongitudinalRebarLayout` exposed top and bottom bar-center collections as `IReadOnlyList<Point2>` but stored caller-provided collection references directly. Mutating those source lists after construction changed a completed layout's bar centers and `Count` without recomputing its cached layer elevations.

## Reserved scope

Make the layout constructor own read-only snapshots of top/bottom bar-center collections. Preserve all cover/layer/spacing/overlap arithmetic, public property types, elevations and planner-generated outputs. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Rebar/BeamLongitudinalRebarPlanner.cs` (`BeamLongitudinalRebarLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/BeamLongitudinalLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to beam reinforcement engineering, bar-count/spacing rules, CAD generation, quantity calculation, UI, or BricsCAD V25 runtime.
- No new numeric validation beyond collection ownership.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `fc8926b7190c163a24b435d366b9376daab15c52` — copy top/bottom bar-center collections into owned read-only snapshots.
- Regression commit: `a13a44464887d3c39854c3a91110c577ef093c3e` — mutate/clear both caller-owned lists and preserve a deterministic 3+3 bar planner result.
- Final observed `main` before close: `e07818b0481838fae3536a75b4ba3ec0ce2efe5f`.
- Validation actually performed:
  - re-fetched current constructor source and confirmed top/bottom lists are copied before exposure;
  - re-fetched the dedicated smoke and confirmed aliasing regression plus normal count/elevation/center-position checks are present;
  - the first smoke create attempt hit a normal concurrent-main `409`; current head was re-fetched and the file was created without force;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25 runtime PASS is claimed.

## Coordination

No current/recent claim was found for `BeamLongitudinalRebarLayout` collection ownership. Existing beam/rebar engineering and native lanes are disjoint from this constructor-only result ownership scope.

## Completion condition

Satisfied: current `main` owns immutable snapshots of top/bottom bar centers, focused regression coverage is present, and this claim is released as `COMPLETED`.
