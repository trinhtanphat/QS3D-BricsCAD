# Work claim — regeneration null regenerator guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-null-regenerator-guard-20260811-2303`
- Registered: `2026-08-11T23:03:00+07:00`
- Baseline main SHA: `8d2d27c2cdc37811a1cc3fd41444446bf933f648`
- Priority: evidence-driven Core constructor invariant hardening during owner-requested `continue all`

## Reserved scope

Make `RegenerationEngine` reject null entries in its regenerator collection at construction time instead of accepting an invalid engine that fails later with `NullReferenceException` during regeneration.

## Expected surfaces

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationConstructorIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationConstructorIntegritySmokeRegistration.cs`
- this claim file for close-out

## Contract

- null graph still fails as today;
- null regenerator enumerable still fails as today;
- an enumerable containing a null regenerator fails immediately with a clear argument exception;
- an empty regenerator list remains valid because measured-solid and quantity-rule paths can still handle work independently.

## Explicit exclusions

No regenerator selection-order changes, regeneration algorithm/pass-count changes, ProjectElement dirty semantics, quantity rules, BricsCAD V25/native/runtime, UI, updater/licensing, persistence, Actions, release, or LOCAL_PASS changes.

## Validation plan

Focused Core smoke covers a null entry and an empty list. Refresh/compare `main` before implementation, publish atomically through a temporary branch/PR if needed, and re-read remote `main` after integration.
