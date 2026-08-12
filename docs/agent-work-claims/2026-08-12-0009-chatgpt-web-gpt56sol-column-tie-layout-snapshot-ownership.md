# Work claim — Column tie layout snapshot ownership

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:09:00+07:00`
- Completed: `2026-08-12T00:11:00+07:00`
- Baseline main SHA: `1e4b6e44a4987835e2bc75abbf6de9092381886d`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`ColumnTieLayout` exposed `ClosedPath` and `ElevationsM` as `IReadOnlyList` values but its public constructor stored both caller-supplied collection references directly. A caller could therefore mutate or clear the original lists after construction and silently change a completed layout's path/elevation set.

## Reserved scope

Make `ColumnTieLayout` own read-only snapshots of both constructor collections. Preserve planner arithmetic, spacing/cover/axial-overlap rules, perimeter/spacing values, public property types and all planner-generated output. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Rebar/ColumnTieLayoutPlanner.cs` (`ColumnTieLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/ColumnTieLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to Column Tie CAD generation, quantity calculation, axial spacing policy, `RebarMath`, UI, or BricsCAD V25 runtime.
- No new engineering/numeric validation beyond collection ownership.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `57a2dba47b864be95e499043270c8036c295d6ab` — copy `ClosedPath` and `ElevationsM` into owned read-only snapshots at construction.
- Regression commit: `39f9044571717514a1364b7317096ab84717acac` — mutate/clear both caller-owned lists after construction and preserve normal planner path/elevation/spacing output.
- Final observed `main` before close: `9fc926e54bb53e6f9247c41de028ce2e51c425ea`.
- Validation actually performed:
  - re-fetched current source and confirmed only constructor collection ownership changed;
  - re-fetched the dedicated smoke and confirmed both collection-alias regressions plus a normal deterministic planner case are present;
  - the first close attempt hit a normal concurrent-main `409`; the current claim blob was re-fetched and no force update was used;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25 runtime PASS is claimed.

## Coordination

Recent Column Tie work covers axial overlap or native generated-rebar revision behavior. Those lanes are completed/disjoint; no current/recent claim was found for `ColumnTieLayout` collection ownership.

## Completion condition

Satisfied: current `main` owns immutable snapshots of constructor path/elevations, focused regression coverage is present, and this claim is released as `COMPLETED`.
