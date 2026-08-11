# Work claim — Rebar notation integer overflow normalization

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:18:00+07:00`
- Baseline main SHA: `31765bef16cac17e73f029c6b17030c9e01e48cb`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`RebarNotationParser` translated overflow from multiplying valid `sets * barsPerSet` into `FormatException`, but oversized digit tokens for `sets` or `quantity` flowed through `int.Parse` and escaped as `OverflowException`. The same malformed notation family therefore exposed inconsistent exception contracts depending on where numeric overflow occurred.

## Reserved scope

Normalize oversized integer tokens in rebar notation to `FormatException` without changing accepted notation syntax, positive-integer semantics, multiplication overflow handling, diameter parsing, whitespace behavior, or rebar planning/generation logic. Add a CAD-independent regression smoke for oversized `sets` and `quantity` tokens and preserve valid boundary parsing.

## Expected surfaces

- `src/QS3D.Core/Rebar/RebarNotationParser.cs`
- `tests/QS3D.Core.SmokeTests/RebarNotationIntegerOverflowSmoke.cs`
- this claim file

## Excluded scope

- No changes to rectangular/slab/wall/beam/column rebar planners, geometry, quantities, CAD adapters, UI, or BricsCAD V25 runtime.
- No changes to notation whitespace rules or compound-segment syntax.
- No GitHub Actions dispatch.

## Validation plan

- Add a deterministic smoke with an integer token above `Int32.MaxValue` in both `qty` and `sets`, asserting `FormatException` rather than leaked `OverflowException`.
- Include a valid `Int32.MaxValue` single-quantity boundary case to prove the parser still accepts the representable upper bound.
- Re-fetch current `main` and target blob before writes; never force-push.
- Hosted environment has no .NET SDK, so record static/source verification and do not claim an executed `dotnet` run.

## Coordination

The recent rebar-notation whitespace claim is `COMPLETED`. The visible rectangular-rebar overlap claim is planner-specific and outside `RebarNotationParser`. No current claim found names parser integer overflow normalization.

## Completion

- Implementation commits:
  - `ea15d972b44918aacf2a41057dd5f15785dd59f2` — replace overflowing `int.Parse` with `int.TryParse` and normalize unrepresentable integer tokens to `FormatException`.
  - `911250a1255bedc58408a2f081e86b535851e56c` — add dedicated smoke covering oversized quantity, oversized set count, multiplied overflow, and the representable `Int32.MaxValue` boundary.
- Final observed `main` before claim close: `69478a0e1e9f8371746647a137c700718ec68226`.
- Validation actually performed:
  - re-fetched `RebarNotationParser.cs` from current `main` and confirmed `PositiveInt` now uses invariant `int.TryParse` and throws `FormatException` on unrepresentable input;
  - re-fetched the new smoke from current `main` and confirmed all four intended assertions plus module initialization are present;
  - confirmed no syntax/whitespace parser patterns, diameter parsing, or rebar planning files were changed;
  - did not execute `dotnet` because the hosted environment does not provide the .NET SDK;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is a CAD-independent parser exception-contract hardening change.

## Completion condition

Satisfied: current `main` normalizes oversized rebar integer tokens to `FormatException`, includes the dedicated regression smoke, and this claim is released as `COMPLETED`.
