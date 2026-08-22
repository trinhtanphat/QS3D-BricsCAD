# Work claim — formula unary recursion depth guard

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-formula-unary-depth`
- Registered: `2026-08-11T22:48:00+07:00`
- Baseline main SHA: `4800cc10afe3c85438fa7a9036387a970846aac2`
- Priority: Concrete reproducible Core robustness defect found during owner-requested full-repository review; long unary chains consume recursive parser frames before the existing depth guard executes.

## Reserved scope

Harden the CAD-independent `ExpressionEvaluator` so unary `+`/`-` recursion is bounded by the parser's existing `MaxDepth` budget before further recursive descent, and add focused regression coverage that proves the evaluator fails early at the depth boundary.

## Expected surfaces

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`
- `docs/agent-work-claims/2026-08-11-2248-chatgpt-web-gpt56sol-formula-unary-depth.md`

## Excluded scope

- No formula-language expansion, variable normalization, case-folding, finite-number semantics, quantity-rule behavior, or unrelated parser rewrite.
- No `SmokeTestRegistration.cs` change; the existing registered formula safety smoke is reused to avoid the shared registration hot spot.
- No BricsCAD V25/native runtime, Ribbon/UI, Direct Draw, persistence, interchange, rebar, updater/installer, CI workflow dispatch, or release work.

## Validation plan

- Add regression coverage for the accepted unary nesting boundary and the first rejected depth.
- Add a long-unary regression that asserts the depth guard trips at the bounded parser position instead of only after consuming the whole unary chain.
- Re-read the final source/test from current remote `main` and inspect the exact pushed commit/diff.
- This runner has no local .NET SDK/BricsCAD runtime, so no unexecuted compile/runtime or V25 gate will be reported as PASS; GitHub Actions will not be dispatched.

## Coordination

The earlier `formula-finite-safety`, `formula-variable-casefold`, and `formula-variable-name-normalization` lanes on `main` are completed and treated as upstream. This reservation is limited to unary recursion depth and the already-registered formula safety smoke file.

## Completion

- Registration commit: `a4ddd66b967bb6af9d13b4483fc05e29b4d3a8b2`.
- Core implementation commit: `bd5fa47af7e46504eb73986b63d6a993b6fa60f0`.
- Regression test commit: `c004a00c71417df76ca4aa3620d57e66924764f7`.
- Final pushed implementation/test state was verified reachable from current `main` after concurrent unrelated commits advanced the branch.

### Delivered behavior

- `ParseUnary` now invokes the existing depth guard before recursive unary descent.
- Unary nesting through depth 64 remains accepted.
- Depth 65 fails closed at parser position 65.
- A 4000-operator unary expression now trips the same depth guard at position 65 rather than consuming the entire unary chain before checking the nesting limit.

### Validation actually performed

- Re-fetched the source and test from current `main` before each write and confirmed no concurrent agent had changed either target blob.
- Used GitHub blob-SHA guarded writes to avoid overwriting concurrent work while `main` was advancing rapidly.
- Added deterministic Core smoke coverage in the already-registered `FormulaFiniteSafetySmoke` runner.
- No GitHub Actions were dispatched.
- No local .NET compilation or runtime smoke was claimed because this execution environment does not provide a local .NET SDK/compiler; no BricsCAD V25 runtime qualification is applicable to this Core-only parser change.

## Completion condition

Satisfied: current `main` contains the early unary-depth guard and focused regression coverage; this reservation is closed as `COMPLETED`.