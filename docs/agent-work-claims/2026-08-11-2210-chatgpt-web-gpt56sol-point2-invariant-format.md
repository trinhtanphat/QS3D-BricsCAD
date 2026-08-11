# Work claim — Point2 invariant diagnostic formatting

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:10:00+07:00`
- Baseline main SHA: `b46e1abce077eb20e393a487e9cfba48980747df`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`Point2.ToString()` currently formats `double` coordinates through the process `CurrentCulture`. On cultures that use a comma decimal separator, the point text becomes locale-dependent and ambiguous with the coordinate separator, so the same Core value produces different diagnostics on different machines.

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

Recent geometry claims were checked. The bulge-overflow lane is already completed and was limited to `BulgeArcTessellator` plus its room-boundary regression; no active claim found names `Point2` or culture-invariant point formatting. This claim intentionally avoids shared smoke registration by using a dedicated module initializer.

## Completion condition

Current `main` contains invariant `Point2.ToString()` formatting plus the dedicated regression guard, and this claim is marked `COMPLETED` with implementation SHA(s), final observed main SHA, and validation actually performed.
