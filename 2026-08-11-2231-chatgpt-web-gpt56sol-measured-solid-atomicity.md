# Work claim — Measured solid quantity atomicity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:31:00+07:00`
- Baseline main SHA: `1cea684c469f1d9acb085025c5c1b7946930c68c`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`MeasuredSolidQuantityPolicy.Apply()` committed a valid measured surface-area quantity before attempting to parse a measured volume property. If surface area was valid but volume was malformed, volume parsing threw after the element had already been partially mutated. The operation therefore violated failure atomicity and could leave quantity state changed despite reporting failure.

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

## Completion

- Implementation commits:
  - `0c4b02cc9b3865be4a493862cf917bc6a6ba369b` — move measured input validation ahead of quantity writes.
  - `f5bdebacb8b94b24883d57fe3caa1b2581d75c68` — make volume validation definite-assignment safe for unsupported categories without changing short-circuit semantics.
  - `7f76086d55b672ec7bb12ea76272046ea528a520` — add dedicated failure-atomicity, successful application, and unsupported-category smoke coverage.
- Final observed `main` before claim close: `8633a08af3b49332231cb24a616082e17a40a98a`.
- Validation actually performed:
  - re-fetched `MeasuredSolidQuantityPolicy.cs` from current `main` and confirmed all applicable inputs are parsed before the first `SetQuantity` call;
  - re-fetched the new smoke from current `main` and confirmed malformed volume preserves the preexisting surface quantity and commits no volume-derived quantities;
  - confirmed valid surface+volume values still populate the same four quantity keys and unsupported categories still ignore volume properties;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core mutation atomicity hardening.

## Completion condition

Satisfied: current `main` validates applicable measured-solid inputs before writes, contains focused regression coverage, and this claim is released as `COMPLETED`.
