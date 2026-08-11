# Work claim — Measured solid quantity atomicity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:31:00+07:00`
- Baseline main SHA: `1cea684c469f1d9acb085025c5c1b7946930c68c`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`MeasuredSolidQuantityPolicy.Apply()` currently commits a valid measured surface-area quantity before attempting to parse a measured volume property. If surface area is valid but volume is malformed, volume parsing throws after the element has already been partially mutated. The operation therefore violates failure atomicity and can leave quantity state changed despite reporting failure.

## Reserved scope

Make measured-solid quantity application validate all applicable measured inputs before mutating any element quantities. Preserve supported-category semantics, metric keys, finite/non-negative validation, and successful output values. Add a CAD-independent regression smoke proving malformed volume does not commit an otherwise-valid surface-area update, while valid surface+volume application still succeeds.

## Expected surfaces

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`
- `tests/QS3D.Core.SmokeTests/MeasuredSolidQuantityAtomicitySmoke.cs`
- this claim file

## Excluded scope

- No changes to quantity rule settings/UI, exporters, BricsCAD measurement capture, selection editing, or native runtime.
- No changes to category support policy or quantity key names.
- No GitHub Actions dispatch.

## Validation plan

- Seed an element with an existing measured-surface quantity, provide a valid surface-area property plus malformed volume, assert `InvalidOperationException`, and verify the preexisting quantity remains unchanged and no volume-derived quantities are committed.
- Cover successful valid surface+volume application and unsupported-category volume behavior.
- Re-fetch current `main` and target blob before writes; never force-push.
- Hosted environment has no usable .NET SDK checkout, so record source/static verification and do not claim an executed repository `dotnet` run.

## Coordination

Recent active claims on Updater, Quantity UI/rules, Browser, Selection and UI were intentionally avoided. No current claim or recent commit was found naming `MeasuredSolidQuantityPolicy` or measured-solid application atomicity.

## Completion condition

Current `main` validates all applicable measured-solid inputs before writes, contains focused regression coverage, and this claim is marked `COMPLETED` with implementation SHA(s) and validation actually performed.
