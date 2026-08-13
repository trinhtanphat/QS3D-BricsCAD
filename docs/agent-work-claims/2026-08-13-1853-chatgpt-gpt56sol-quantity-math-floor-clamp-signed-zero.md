# Work claim — QuantityMath floor/clamp signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-math-floor-clamp-signed-zero-20260813`
- Registered: `2026-08-13T18:53:00+07:00`
- Baseline main SHA: `ed60f400d321474640be2682d7093d4abc54df34`
- Priority: P0 deterministic Core quantity canonicality hardening for the net48 V25 runtime boundary.

## Confirmed defect

`QS3D.Core` targets `netstandard2.0` and the V25 adapter targets `net48` while project-referencing Core. On the .NET Framework implementation used by the V25 runtime, floating `Math.Max`/`Math.Min` return the second operand when zero-valued operands compare equal, so signed zero is not automatically canonicalized.

Two remaining `QuantityMath` surfaces rely on those helpers without a final zero canonicalization:

- `SubtractFloorZero(-0d, 0d, ...)` computes negative zero and then calls `Math.Max(0d, result)`, which can preserve the second operand `-0d` on .NET Framework;
- `Clamp(-0d, 0d, positive, ...)` can preserve `-0d` through `Math.Min(maximum, value)` and then `Math.Max(minimum, ...)` for the same equal-zero behavior.

`Positive()` and the zero path of `Hypot()` already return literal `0d`, so they are excluded.

## Reserved scope

- `src/QS3D.Core/Services/QuantityMath.cs`
- `tests/QS3D.Core.SmokeTests/QuantityMathUnderflowSmoke.cs`
- this claim file for closeout

## Intended change

- canonicalize the already finite/floored `SubtractFloorZero()` return whenever it compares equal to zero;
- canonicalize the already validated `Clamp()` return whenever it compares equal to zero;
- preserve subtraction overflow, clamp finite/bounds validation, ordinary positive values and nonzero floor/clamp behavior;
- add bit-level regression cases through the existing reflection smoke for the exact negative-zero counterexamples plus ordinary positive sanity cases.

## Excluded scope

- completed QuantityMath Multiply/Divide and Add signed-zero lanes;
- `Positive`/`Hypot`, which already return literal positive zero on zero paths;
- generic `Math.Max/Min` wrappers or changes outside QuantityMath;
- business formulas, UI/export, adapter-native operations, Actions/release/runtime qualification.

## Coordination

- Previous Add claim closed at `ed60f400d321474640be2682d7093d4abc54df34` before this claim.
- Exact recent commit searches for `QuantityMath SubtractFloorZero signed zero` and `QuantityMath Clamp signed zero` returned no competing lanes.
- Target/runtime evidence was checked before claim: Core `netstandard2.0`, V25 `net48` + ProjectReference to Core, and Microsoft .NET Framework Reference Source floating `Math.Max` semantics.

## Validation plan

- refresh `main` after claim and recheck QuantityMath history before source mutation;
- keep production changes to final zero canonicalization on the two demonstrated methods;
- retain all prior QuantityMath signed-zero/underflow regressions;
- re-fetch exact source/test diffs and close with managed/native execution marked `NOT_RUN` when unavailable.

## Completion condition

`SubtractFloorZero` and `Clamp` no longer leak signed negative zero on the legacy V25 runtime boundary, existing validation/nonzero behavior remains intact, focused bit-level coverage is on `main`, and this claim closes with exact readback and no fabricated runtime PASS.
