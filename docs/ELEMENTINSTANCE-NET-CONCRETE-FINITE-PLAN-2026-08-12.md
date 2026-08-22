# ElementInstance Net Concrete Finite Closure Plan — 2026-08-12

## Goal

Keep `ElementInstance` closed over its documented finite-measurement domain when deriving `NetConcreteM3` from two finite stored operands.

## Defect

`GrossConcreteM3` and `DeductionM3` individually reject `NaN` and infinities, but IEEE-754 subtraction can overflow even when both operands are finite. The current expression-bodied `NetConcreteM3` therefore permits a public derived measurement to become non-finite.

## Implementation

1. Preserve the existing setter validation and the exact `gross - deduction` arithmetic for normal values.
2. Compute the derived value once in the `NetConcreteM3` getter.
3. Fail closed with an arithmetic/overflow exception when the derived result is non-finite.
4. Do not impose a new non-negative rule: negative finite net values retain current behavior because that is outside this defect's scope.
5. Add a focused Core smoke regression covering normal arithmetic and overflow from individually finite operands.

## Safety / overlap

- No edits to reporting, export, `ProjectElement`, quantity rules, persistence, BricsCAD adapters, or UI.
- No GitHub Actions dispatch and no release publication.
- Re-read `main` and exact file blobs before each write; never force concurrent work.

## Verification

- Source regression proves finite result preservation and non-finite result rejection.
- Re-fetch committed source/test blobs.
- Compare implementation/test/closure commits to latest `main`; require `behind_by: 0` before closing the claim.
- Runtime BricsCAD V25 remains outside this Core-only source regression and is not claimed as PASS.
