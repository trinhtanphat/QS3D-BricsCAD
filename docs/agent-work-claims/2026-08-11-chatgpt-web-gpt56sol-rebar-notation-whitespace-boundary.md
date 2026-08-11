# Agent work claim — rebar notation whitespace boundary

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11T21:47:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `3c4a6a4736d4f2ebc081b68f2ec602514615baae`
- Priority: source-safe Core parsing hardening; prevent whitespace inside numeric tokens from being silently removed and changing rebar quantities/diameters/spacing.

## Confirmed defect

`RebarNotationParser.Parse(...)` currently runs `notation.Replace(" ", string.Empty)` before splitting compound notation. That changes token meaning: an invalid/accidental input such as `2 0D16` becomes `20D16` and is accepted as twenty D16 bars. The parser regexes already allow whitespace at legitimate token boundaries, so globally deleting spaces is unnecessary and can silently alter quantities.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarNotationParser.cs`
- `tests/QS3D.Core.SmokeTests/RebarNotationWhitespaceRegressionSmoke.cs`
- this claim file for close-out

## Functional contract

- preserve input whitespace while parsing instead of concatenating separated numeric fragments;
- reject whitespace embedded inside a numeric literal such as `2 0D16` or `D1 6@150`;
- continue accepting legitimate formatting whitespace such as `4 Ø20`, `D8 @ 150`, and whitespace around `+`/`x` separators;
- preserve existing supported notation forms, checked multiplied quantities, invariant-culture numeric parsing, and exception behavior for ordinary malformed notation;
- do not touch schedule arithmetic, CAD/native builders, UI, persistence, or other active claims.

## Validation target

- behavioral Core smoke proving embedded numeric whitespace fails instead of being concatenated;
- behavioral Core smoke proving legitimate boundary whitespace still parses with the original values;
- use the established net8 Core smoke `[ModuleInitializer]` registration pattern;
- no GitHub Actions dispatch and no remote BricsCAD V25 runtime PASS claim.

## Completion condition

Parser normalization no longer changes numeric token meaning, focused behavioral regression is merged on current `main`, source is re-fetched after concurrent updates, and this claim is marked `COMPLETED` with exact implementation/test SHAs.
