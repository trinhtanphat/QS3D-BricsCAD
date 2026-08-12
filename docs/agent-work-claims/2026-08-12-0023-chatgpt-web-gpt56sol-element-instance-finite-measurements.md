# Work claim — ElementInstance finite stored measurements

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:23:00+07:00`
- Completed: `2026-08-12T00:26:00+07:00`
- Baseline main SHA: `42ad446c6d70ba4462e4c830e83d16733aa368e1`
- Claim commit: `f19d3786d444a4a86cfb7e53a7f7ec4405804629`
- Priority: evidence-driven remote-safe Core domain integrity

## Confirmed defect

`ElementInstance` exposed thirteen public stored measurement `double` auto-properties that accepted `double.NaN` and infinities, allowing non-finite measurement state to persist in a public Core domain instance.

## Completed scope

All thirteen stored numeric measurement setters now require finite `double` values. Existing zero defaults and every finite value, including negative values, remain accepted. The `NetConcreteM3` derived subtraction remains unchanged.

Covered stored properties:

- `LengthM`
- `AreaM2`
- `VolumeM3`
- `GrossConcreteM3`
- `DeductionM3`
- `FormworkM2`
- `DoorAreaM2`
- `OuterPerimeterM`
- `InnerPerimeterM`
- `SideAreaM2`
- `BottomAreaM2`
- `TopAreaM2`
- `OtherAreaM2`

## Product/test commits

- `77f514eaf8c16536da06398dea808370f4c0fd36` — `fix(domain): reject non-finite element measurements`
- `fdc992439cf16653a7e7972c0886b8138a397bb8` — `test(domain): cover finite element measurements`
- `06f362d0bc22db420b9561ad021e023038d83ebf` — `test(domain): register element measurement smoke`

## Validation

- Re-fetched the current target blob after claim publication before the source write.
- Reviewed exact source diff: only backing storage/setters plus one shared finite guard were added; identity, family, floor, source handles and `NetConcreteM3` formula were preserved.
- Smoke verifies all thirteen zero defaults, representative negative/positive finite values, NaN/+Infinity/-Infinity rejection across every stored measurement, preservation of the prior value after each failed assignment, and unchanged normal `NetConcreteM3` behavior.
- Registration uses a dedicated module initializer to avoid shared smoke registry contention.
- After registration, observed `main` at `48b0a57a3463d0c0d22ce80a9406faf84d83807b`; comparison from `06f362d0bc22db420b9561ad021e023038d83ebf` reported `status=ahead`, `behind_by=0`, with merge base equal to the registration commit. Concurrent commits touched other surfaces.
- GitHub Actions were not dispatched.
- No .NET SDK or BricsCAD V25 runtime PASS is claimed from this hosted session.

## Excluded scope

- No `ProjectElement.cs` changes.
- No reporting nonnegative policy, quantity formulas, rounding/tolerance or unit semantics.
- No derived subtraction overflow policy.

## Completion

The finite stored-measurement invariant and focused regression source are on current `main`; claim released as completed.