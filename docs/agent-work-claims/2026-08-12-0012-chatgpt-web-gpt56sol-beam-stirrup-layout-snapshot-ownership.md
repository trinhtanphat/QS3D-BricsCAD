# Work claim — Beam stirrup layout snapshot ownership

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:12:00+07:00`
- Completed: `2026-08-12T00:15:00+07:00`
- Baseline main SHA: `8f6ec49c30f391c77aa2ca32a7184559d180136c`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`BeamStirrupLayout` exposed `StationOffsetsM` and `SectionLoop` as read-only lists but its constructor chain ultimately stored the caller-supplied references directly. A caller could mutate the source collections after construction, changing `Count` or the section path of an already-computed layout while cached spacing/length values remained unchanged.

## Reserved scope

Make the shared `BeamStirrupLayout` constructor own read-only snapshots of station offsets and section path. Preserve all station/cover/spacing/bend/hook/tessellation arithmetic, cached length values, public property types and planner-generated outputs. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs` (`BeamStirrupLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/BeamStirrupLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to beam stirrup engineering/spacing/hook/bend rules, tessellation, CAD generation, quantity calculation, UI, or BricsCAD V25 runtime.
- No new numeric validation beyond collection ownership.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `ec8fe134227cd01ba8592bcf59b387d666988fd8` — copy station offsets and section path into owned read-only snapshots in the shared constructor.
- Regression commit: `406826194c5f69133117fc5440afb23bca7d3de8` — mutate/clear both legacy constructor source lists and preserve a normal no-bend/no-hook planner result.
- Final observed `main` before close: `07c986cc4419eae81d11adf505b4586f7247c030`.
- Validation actually performed:
  - re-fetched the current constructor region and confirmed the two collections are copied before being exposed;
  - re-fetched the dedicated smoke and confirmed source-list aliasing plus normal legacy planner cardinality/length/spacing checks are present;
  - the first source write hit a normal concurrent-main `409`; current head/blob were re-fetched and the write was retried without force;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25 runtime PASS is claimed.

## Coordination

No current/recent claim was found for `BeamStirrupLayout` collection ownership. Other beam-stirrup work concerns engineering/CAD behavior and is disjoint from this constructor-only result ownership lane.

## Completion condition

Satisfied: current `main` owns immutable snapshots of constructor stations/section path, focused regression coverage is present, and this claim is released as `COMPLETED`.
