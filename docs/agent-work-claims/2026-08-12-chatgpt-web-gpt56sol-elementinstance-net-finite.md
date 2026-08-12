# Work claim — ElementInstance net concrete finite closure

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `62beefb3f90e7459f32bf2cdbf6181c017cbfbca`
- Priority: owner-requested whole-repository audit; Core numeric integrity

## Verified defect

`ElementInstance` validates every stored measurement assigned through its setters with `RequireFinite(...)`, but `NetConcreteM3` was computed as an unchecked floating-point subtraction of `GrossConcreteM3 - DeductionM3`. Two individually finite doubles can overflow on subtraction, allowing the public derived quantity to become non-finite despite the type's finite-measurement contract.

## Delivered

- Planning: `34a9cea7d52c1afede22abb22d4ae8766ba28f1a`
- Source fix: `1b3e08cb64938dd1ec3ff99ce966882044409081`
- Focused smoke regression: `e42dd7be1d17902006b857b3d223a08ec1530f36`
- `NetConcreteM3` now computes once and throws `OverflowException` if the arithmetic result is non-finite.
- Finite results remain unchanged, including negative finite results.
- The smoke covers normal finite arithmetic, negative finite arithmetic, positive overflow and negative overflow using individually finite operands.

## Reserved scope

- `src/QS3D.Core/Domain/ElementInstance.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceNetConcreteFiniteSmoke.cs`
- `docs/ELEMENTINSTANCE-NET-CONCRETE-FINITE-PLAN-2026-08-12.md`
- this claim file

## Verification

- Exact committed source blob re-fetched from `main`: `c5db157cd434f2b016c82947af33585d386633ca`.
- Exact smoke blob re-fetched from `main`: `f15afc973298b8c21ffa231defcc9c96d61f4e8c`.
- Against observed `main` `8b81c0041f07789c4eb044bcfe44be470fc589b7`, source commit `1b3e08c...` had `behind_by: 0`; smoke commit `e42dd7b...` had `behind_by: 0`.
- No concurrent commits in those ancestry comparisons modified `src/QS3D.Core/Domain/ElementInstance.cs` after the source fix.
- Smoke coverage is committed but was **not executed through GitHub Actions in this session**; no CI/runtime PASS is claimed.
- No GitHub Actions dispatched, no release published, and no licensed BricsCAD V25 runtime PASS claimed.
