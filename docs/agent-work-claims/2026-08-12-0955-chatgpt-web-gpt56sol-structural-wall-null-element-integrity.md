# Work claim — Structural wall null element integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `eea7eefdb45e7548be7b1abdd06d7a690ac0dbf5`
- Priority: evidence-driven Core malformed-state execution integrity

## Confirmed defect

`ProjectState.FindElement(...)` explicitly fails closed when `ProjectState.Elements` contains a null semantic element, but `StructuralRegenerator.LinkedOpeningArea(...)` currently dereferences every entry through `child.Category`. A malformed project containing an unrelated null element can therefore surface an accidental `NullReferenceException` during structural-wall regeneration instead of the repository's stable malformed-project `InvalidOperationException` contract.

Because linked-opening area is computed before structural-wall `SetQuantity(...)` calls, this can be fixed without changing quantity semantics or leaving partial wall quantities.

## Intended scope

- make structural-wall linked-opening enumeration reject null semantic elements explicitly before dereference;
- preserve canonical/missing/empty `HostWallId` behavior from PR #721;
- preserve valid opening deduction, case-insensitive canonical host matching, quantity formulas and read-only rejection before wall quantity mutation;
- add one focused Core smoke regression.

## Reserved surfaces

- `src/QS3D.Core/Services/StructuralRegenerator.cs`
- `tests/QS3D.Core.SmokeTests/StructuralWallNullElementIntegritySmoke.cs`
- this claim file

## Excluded scope

Do not modify generated rebar ownership, curtain geometry, ED2, Grid Annotation, Zone assignment, recognition, opening native cut, UI/CAD adapters, build/release workflows, or other currently claimed lanes.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual execution.
