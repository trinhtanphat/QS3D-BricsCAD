# Work claim — Linear rebar layout snapshot ownership

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:06:00+07:00`
- Completed: `2026-08-12T00:08:00+07:00`
- Baseline main SHA: `9e12cb7f1145659c84ed8fac4d033c8832007a68`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`LinearRebarLayout` exposed `OffsetsM` as `IReadOnlyList<double>` but its public constructor stored the caller-supplied list reference directly. A caller could therefore construct a valid layout from a mutable `List<double>` and later mutate or clear that source list, changing `OffsetsM` and `Count` after construction. This broke the result object's snapshot semantics and could invalidate a previously planned layout without going through the planner.

## Reserved scope

Make `LinearRebarLayout` take an owned read-only snapshot of constructor offsets. Preserve all planner arithmetic, spacing/cover/bar-count rules, public property types, valid offsets and generated layout values. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs` (`LinearRebarLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/LinearRebarLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to linear rebar spacing, physical overlap rules, `RebarMath`, CAD generation, quantity calculation, UI, or BricsCAD V25 runtime.
- No new numeric/engineering validation beyond ownership of the supplied offsets collection.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `6d66395536ee219a661e8b0e0aaf03a86abca1a0` — copy caller offsets into an owned `ReadOnlyCollection` at `LinearRebarLayout` construction.
- Regression commit: `96fcef6c338aea58831f2be71d66e8929d8d5b0f` — mutate and clear the caller-owned list after construction and assert retained count/offset values; preserve a deterministic 3-bar planner result.
- Final observed `main` before close: `104c75a48edbddfb6108912cf2088e1757bf02b5`.
- Validation actually performed:
  - re-fetched current source and confirmed the only product change is constructor collection ownership;
  - re-fetched the dedicated smoke and confirmed both aliasing regression and normal planner output checks are present;
  - the first source update hit a normal concurrent-main `409`; current source/head were re-fetched and the update was retried without force;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25 runtime PASS is claimed.

## Coordination

The prior linear-rebar physical-spacing claim is `COMPLETED` and addressed spacing/overlap semantics. No current/recent claim was found for `LinearRebarLayout` collection ownership/aliasing.

## Completion condition

Satisfied: current `main` owns an immutable snapshot of constructor offsets, focused regression coverage is present, and this claim is released as `COMPLETED`.
