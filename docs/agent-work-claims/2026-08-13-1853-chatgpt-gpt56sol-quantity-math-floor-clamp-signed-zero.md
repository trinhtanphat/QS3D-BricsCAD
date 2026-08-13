# Work claim — QuantityMath floor/clamp signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-math-floor-clamp-signed-zero-20260813`
- Registered: `2026-08-13T18:53:00+07:00`
- Completed: `2026-08-13T18:56:00+07:00`
- Baseline main SHA: `ed60f400d321474640be2682d7093d4abc54df34`
- Priority: P0 deterministic Core quantity canonicality hardening for the net48 V25 runtime boundary.

## Confirmed defect

`QS3D.Core` targets `netstandard2.0` and the V25 adapter targets `net48` while project-referencing Core. On the .NET Framework implementation used by the V25 runtime, floating `Math.Max`/`Math.Min` use ordinary comparisons and return the second operand when equal, so signed zero is not automatically canonicalized.

Before this lane:

- `SubtractFloorZero(-0d, 0d, ...)` computed negative zero and then called `Math.Max(0d, result)`, which can preserve the second operand `-0d` on .NET Framework;
- `Clamp(-0d, 0d, positive, ...)` could preserve `-0d` through the `Math.Min`/`Math.Max` chain for the same equal-zero behavior.

`Positive()` and the zero path of `Hypot()` already return literal `0d`, so they remain unchanged.

## Implemented scope

- `SubtractFloorZero()` now returns the positive result only when `result > 0d`; every zero/negative floored result returns literal `+0d` after the existing finite check.
- `Clamp()` still performs the same finite and bounds validation and the same min/max clamping; it now canonicalizes the already-clamped result to literal `+0d` whenever it compares equal to zero.
- Ordinary positive subtraction/clamp behavior remains unchanged.
- The existing QuantityMath reflection smoke now checks the exact negative-zero counterexamples with `BitConverter.DoubleToInt64Bits`, plus a negative-to-floor-zero case and ordinary positive clamp/subtraction cases.
- All prior Multiply/Divide/Add signed-zero, subnormal and underflow regressions remain present.

## Excluded scope

- completed QuantityMath Multiply/Divide and Add signed-zero lanes;
- `Positive`/`Hypot`, which already return literal positive zero on zero paths;
- generic `Math.Max/Min` wrappers or changes outside QuantityMath;
- business formulas, UI/export, adapter-native operations, Actions/release/runtime qualification.

## Coordination / moving-main reconciliation

- Previous Add claim closed at `ed60f400d321474640be2682d7093d4abc54df34` before this lane began.
- Immediately before this claim was created, concurrent commit `ed457334b6a90792c910327ed202b1975dff3df8` landed and became the actual parent of the claim commit. It reserves only `MeasurementTrace.cs` and `MeasurementTraceContractSmoke.cs` for canonical `none` policy handling and explicitly excludes QuantityMath; therefore it is disjoint and no overwrite occurred.
- Claim commit: `a600b6160787d0799d84e3747ac20fc763ef0711`.
- Production fix: `5a1cd527b522d3109fb0ee7ee252575859bd8c18` — `fix(core): canonicalize QuantityMath floor and clamp zero`.
- Focused regression: `001bb0271b208b3261fa4290013d487bb6be50de` — `test(core): guard QuantityMath floor and clamp signed zero`.
- Post-regression refresh showed `main` exactly at `001bb0271b208b3261fa4290013d487bb6be50de`; no concurrent commit touched the two reserved source/test files before closeout.

## Validation actually executed

- Target/runtime contract readback: `QS3D.Core` is `netstandard2.0`; `QS3D.BricsCAD.V25` is `net48` and project-references Core.
- Microsoft .NET Framework Reference Source was inspected for floating `Math.Max`/`Math.Min` tie behavior before the source change, establishing the legacy signed-zero counterexample relevant to V25.
- Exact production readback confirmed blob `dc54b91e505e663296ce72fb912a40fefa41647c`; only `SubtractFloorZero` and `Clamp` zero-return behavior changed in this lane.
- Exact regression readback confirmed blob `9263e67f3b86157ba1b014999f78940c3b6f7c55`, including `SubtractFloorZero(-0d, 0d)`, floored negative subtraction, `Clamp(-0d, 0d, 1d)`, ordinary subtraction/clamp cases, and all earlier QuantityMath coverage.
- Hosted environment has no `dotnet`, `csc`, `mcs` or `msbuild`, so managed compile/smoke execution was `NOT_RUN`; no managed PASS is claimed.
- No GitHub Actions, packaging, installed-reference adapter build or licensed BricsCAD V25 runtime qualification was dispatched/executed.

## Completion condition

Satisfied for this bounded Core source/static lane: `SubtractFloorZero` and `Clamp` no longer leak signed negative zero at the legacy V25 runtime boundary, existing validation/nonzero behavior remains intact, focused bit-level coverage is on `main`, moving-main concurrency was explicitly reconciled, and unavailable managed/native gates remain unclaimed.
