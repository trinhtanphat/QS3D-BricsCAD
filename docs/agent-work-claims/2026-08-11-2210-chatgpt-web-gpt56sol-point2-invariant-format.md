# Work claim — Point2 invariant diagnostic formatting

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:10:00+07:00`
- Baseline main SHA: `b46e1abce077eb20e393a487e9cfba48980747df`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`Point2.ToString()` formatted `double` coordinates through the process `CurrentCulture`. On cultures that use a comma decimal separator, the point text became locale-dependent and ambiguous with the coordinate separator, so the same Core value produced different diagnostics on different machines.

## Reserved scope

Make `Point2` diagnostic string formatting culture-invariant without changing coordinate storage, equality, hashing, distance math, finite-input policy, or public geometry behavior beyond `ToString()` text stability. Add a CAD-independent regression guard that proves the output remains invariant while the ambient culture uses a comma decimal separator.

## Expected surfaces

- `src/QS3D.Core/Geometry/Point2.cs`
- `tests/QS3D.Core.SmokeTests/Point2InvariantFormattingSmoke.cs`
- `tests/QS3D.Core.SmokeTests/Point2InvariantFormattingRegistration.cs`
- this claim file

## Excluded scope

- No changes to `BulgeArcTessellator`, room-boundary tessellation, polygon topology, rebar planners, quantity/reporting, persistence, updater, UI, or BricsCAD V25 adapters/runtime.
- No changes to `Point2.DistanceTo`, equality, hashing, constructors, numeric precision, or validation policy.
- No GitHub Actions dispatch.

## Validation plan

- Add a deterministic Core smoke that temporarily installs a comma-decimal culture, checks exact invariant point text, and restores the previous culture in `finally`.
- Re-fetch current `main` and all target files before each write and never force-push.
- Hosted environment has no .NET SDK, so record source-level/static verification here and do not claim an executed `dotnet` run.

## Coordination

Recent geometry claims were checked. The bulge-overflow lane was already completed and was limited to `BulgeArcTessellator` plus its room-boundary regression; no active claim found named `Point2` or culture-invariant point formatting. This claim intentionally avoided shared smoke registration by using a dedicated module initializer.

## Completion

- Implementation commits:
  - `84938def700c08108f5d78f81da4b37887c1ecdd` — make `Point2.ToString()` use `CultureInfo.InvariantCulture` while preserving its existing general numeric format.
  - `07386e8c93512acb99ff8dcfc138695b3bf05142` — add comma-decimal regression smoke with guaranteed culture restoration.
  - `71b0b5bcd1b15cab64c3576876095251412f376c` — register the smoke through a dedicated module initializer.
- Final observed `main` before claim close: `94c2b5f84a1d6184e921fb7d686a0abfdef8022f`.
- Validation actually performed:
  - re-fetched `src/QS3D.Core/Geometry/Point2.cs` from current `main` and confirmed the invariant formatter is present;
  - re-fetched both new smoke sources from current `main` and confirmed the comma-decimal setup, exact ordinal assertion, `finally` restoration, and module registration are present;
  - confirmed `QS3D.Core` targets `netstandard2.0` and the implementation uses APIs available there;
  - did not execute `dotnet` because the hosted environment does not provide the .NET SDK;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this change is CAD-independent Core diagnostic formatting and does not alter a BricsCAD/native runtime contract.

## Completion condition

Satisfied: current `main` contains invariant `Point2.ToString()` formatting plus the dedicated regression guard. This claim is released as `COMPLETED`.
