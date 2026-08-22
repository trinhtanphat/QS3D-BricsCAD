# Work claim — single-grid intersection input validation

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:38:00+07:00`
- Baseline main SHA: `29bbffe36ad42d000ad93c573bff36b7c49166d9`
- Claim commit: `17e328b5e363f785917f818b90805247cbef4b53`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect fixed

`GridIntersectionPlanner.FindIntersections` materialized input and returned an empty intersection set immediately when fewer than two curves were supplied. Its per-curve `Validate` loop ran only after that return. A single malformed Grid reference could therefore be silently accepted as “no intersections”, while the same curve was rejected once a second curve was present.

The fewer-than-two-curves return now runs only after every supplied curve has passed the existing validation and duplicate-id loop. Empty input still returns an empty set, a valid single curve still returns an empty set, and pair intersection mathematics are unchanged.

## Implementation surfaces

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridIntersectionSingleInputValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridIntersectionSingleInputValidationRegistration.cs`
- this claim file

## Product commits

- `eefe5964b072851433875c1cb090eb0c4d671344` — `fix(core): validate single grid intersection input`
- `6f6670387df03c7b9864fe76fcd92f98ec56c4d4` — `test(core): cover single grid input validation`
- `e476abf38300b5df713277e56f6da3f5db1e25bb` — `test(core): register single grid input validation smoke`

## Regression coverage

The focused Core smoke covers:

- empty input remains a valid empty result;
- one valid Grid LINE remains a valid empty result;
- one degenerate Grid LINE fails closed with the existing validation contract;
- one Grid LINE with a non-finite endpoint fails closed with the existing validation contract.

Registration uses a dedicated module initializer and does not touch shared smoke registration.

## Validation truth

- Exact implementation diff was re-read after push and contains only one semantic change: moving the `< 2` early return from before the validation loop to after it. No intersection formula, tolerance or helper changed.
- Exact smoke and module-registration diffs were re-read after push.
- After registration, comparison from `e476abf38300b5df713277e56f6da3f5db1e25bb` to observed `main` `7599285623eb509aaf1fea96af765ae08f3baf33` reported `behind_by: 0`; intervening commits touched disjoint EntitySnapshot/Revision/UI/preflight/claim surfaces.
- Hosted environment has no .NET SDK, so the smoke suite was not executed in this session.
- No GitHub Actions were dispatched and no BricsCAD V25 runtime/build PASS is claimed.

## Exclusions respected

No Grid naming/renumbering/annotation, Grid identity-token, browser/UI, source-reconcile, native V25 adapter, reporting, quantity, persistence, rebar or project mutation files were changed.

## Completion condition

Satisfied for remote/source scope: all supplied Grid references now reach the existing validation contract regardless of cardinality, focused regression source is registered on `main`, concurrent work was preserved, and this claim is released as `COMPLETED`.