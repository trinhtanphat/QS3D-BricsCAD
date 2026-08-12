# Work claim — Rebar schedule structural read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-schedule-readonly-result-20260812-0803`
- Registered: `2026-08-12T08:03:00+07:00`
- Baseline main SHA: `e391d9c2f44d48e6b66daa7e2e75736ed5eadd97`
- Priority: evidence-driven public result ownership during owner-requested `continue all`

## Confirmed defect

`RebarScheduleBuilder.Build(IEnumerable<RebarScheduleInput>)` returns an `IReadOnlyList<RebarScheduleRow>` but currently returns the mutable backing `List<RebarScheduleRow>` directly. A caller can cast the result back to `List<RebarScheduleRow>` / mutable collection and structurally add, remove or clear rows after the schedule has been built. Neighboring Core result APIs wrap their completed list snapshots before returning them.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarSchedule.cs` — return boundary of `RebarScheduleBuilder.Build` only.
- `tests/QS3D.Core.SmokeTests/RebarScheduleReadOnlyResultSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Return a structural read-only wrapper for the completed schedule list while preserving row ordering, row objects, quantity/spacing math, aggregate validation, project schedule behavior and every existing exception contract. No deep-immutability redesign of `RebarScheduleRow` is included.

## Excluded scope

No BBS export UI, modeless ownership, quantity arithmetic, notation parsing, generated rebar, CAD/native behavior, Level placement, release/update or persistence changes.

## Validation plan

Prove ordinary count and spacing schedules still produce the same rows/order and that the returned collection cannot be structurally mutated through `ICollection<RebarScheduleRow>`. Re-fetch source before write; never force-push. No GitHub Actions dispatch and no BricsCAD runtime qualification claim.
