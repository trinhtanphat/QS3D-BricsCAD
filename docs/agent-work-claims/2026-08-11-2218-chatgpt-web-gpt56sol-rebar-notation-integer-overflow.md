# Work claim — Rebar notation integer overflow normalization

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:18:00+07:00`
- Baseline main SHA: `31765bef16cac17e73f029c6b17030c9e01e48cb`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`RebarNotationParser` translates overflow from multiplying valid `sets * barsPerSet` into `FormatException`, but oversized digit tokens for `sets` or `quantity` flow through `int.Parse` and escape as `OverflowException`. The same malformed notation family therefore exposes inconsistent exception contracts depending on where numeric overflow occurs.

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

The recent rebar-notation whitespace claim is `COMPLETED`. The currently visible rectangular-rebar overlap claim is planner-specific and outside `RebarNotationParser`. No current claim found names parser integer overflow normalization.

## Completion condition

Current `main` normalizes oversized rebar integer tokens to `FormatException`, includes the dedicated regression smoke, and this claim is marked `COMPLETED` with implementation SHA(s) and validation actually performed.
