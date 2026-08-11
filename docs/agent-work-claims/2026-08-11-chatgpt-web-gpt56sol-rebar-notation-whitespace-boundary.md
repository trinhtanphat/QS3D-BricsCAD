# Agent work claim — rebar notation whitespace boundary

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:47:00+07:00
- Completed: 2026-08-11T21:50:00+07:00
- Status: `COMPLETED`
- Baseline main SHA: `3c4a6a4736d4f2ebc081b68f2ec602514615baae`
- Priority: source-safe Core parsing hardening; prevent whitespace inside numeric tokens from being silently removed and changing rebar quantities/diameters/spacing.

## Confirmed defect

`RebarNotationParser.Parse(...)` ran `notation.Replace(" ", string.Empty)` before splitting compound notation. That changed token meaning: an invalid/accidental input such as `2 0D16` became `20D16` and was accepted as twenty D16 bars. The parser regexes already allow whitespace at legitimate token boundaries, so globally deleting spaces was unnecessary and could silently alter quantities.

## Implemented

- `bd22db3e66d7ce5cbf7ad9d08bf52abd8beb8f8e` — `fix(rebar): preserve notation whitespace boundaries`
  - removes global space deletion;
  - splits compound notation directly from the original input;
  - keeps the existing anchored regexes responsible for accepting only legitimate whitespace positions.
- `6be8c83031ba5b305a2692287c18e2519baa11a9` — `test(core): guard rebar notation whitespace boundaries`
  - rejects `2 0D16` instead of concatenating it into `20D16`;
  - rejects `D1 6@150` instead of concatenating the diameter;
  - preserves valid `4 Ø20`, `D8 @ 150`, and whitespace around compound/multiplied separators.

## Validation evidence

- Re-fetched `src/QS3D.Core/Rebar/RebarNotationParser.cs` from a newer current `main`; it now uses `notation.Split(...)` directly and no longer removes spaces.
- Re-fetched `tests/QS3D.Core.SmokeTests/RebarNotationWhitespaceRegressionSmoke.cs` from the same newer tree; focused public-parser regression remains intact.
- The Core smoke project is SDK-style net8 and the repository already uses `[ModuleInitializer]` registration for focused Rebar smoke files.
- Concurrent main updates were on unrelated PlanTo3D/Xref/reference-search lanes and did not overwrite the reserved parser/test surfaces.
- No GitHub Actions workflow was dispatched and no smoke executable run is claimed from this connector-only lane.
- No BricsCAD V25/native runtime claim is required for this pure Core parser invariant.

## Reserved scope honored

- Changed only `RebarNotationParser.cs`, the focused Core smoke file, and this claim close-out.
- Did not change schedule arithmetic, shape builders, CAD/native generation, UI, persistence, quantities/reporting, updater, Ribbon, Direct Draw, or other active claims.

## Completion

Completed. Rebar notation whitespace can no longer silently concatenate numeric fragments, while legitimate formatting whitespace remains supported; exact implementation and regression SHAs are recorded above.
