# Work claim — OpeningPropertySet sill-offset signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-opening-sill-offset-signed-zero-20260813`
- Registered: `2026-08-13T19:10:00+07:00`
- Completed: `2026-08-13T19:12:00+07:00`
- Baseline main SHA: `a5a4acaa70ecaf93c61c09606002ff65e76f7169`
- Priority: P0 deterministic Core opening-metric canonicality.

## Confirmed defect

`OpeningPropertySet.SillOffsetMm` is the one opening metric that legitimately accepts zero and negative values. Its setter delegates to `RequireFinite()`, which rejected NaN/infinity but previously returned the raw finite double. IEEE-754 `-0d` could therefore be stored in the backing field and exposed unchanged by the getter.

`WidthMm`, `HeightMm`, and `ThicknessMm` delegate through the same finite helper but are subsequently required to be strictly greater than zero, so this lane does not change their acceptance contract.

## Implemented scope

- `RequireFinite()` still rejects NaN/infinity and now canonicalizes every accepted finite zero to literal `+0d`.
- `SillOffsetMm` therefore stores/exposes canonical positive zero for `-0d` input.
- Legal negative nonzero sill offsets remain unchanged.
- `RequirePositiveFinite()` still applies `<= 0d` after finite normalization, so Width/Height/Thickness still reject both `+0d` and `-0d`.
- New registered `OpeningPropertySetSignedZeroSmoke` bit-checks `SillOffsetMm = -0d`, verifies negative/positive nonzero sill offsets, verifies ordinary positive dimensions, rejects non-finite sill offsets, and guards strict-positive dimension refusal.

## Excluded scope

- Width/Height/Thickness business constraints or defaults;
- opening host/rehost/cut geometry, Door/Opening commands, persistence/export/UI;
- QuantityMath, ElementInstance, ProjectElement and other signed-zero lanes already completed;
- the concurrent Formula evaluator signed-zero lane, which reserves only Formula source/test files;
- GitHub Actions, packaging, release and licensed BricsCAD runtime qualification.

## Coordination / moving-main reconciliation

- Exact recent commit searches for `OpeningPropertySet signed zero` and `SillOffset signed zero` returned no competing lane.
- The first claim write was rejected with HTTP 409 because moving `main` advanced; no claim/source/test write occurred in that failed attempt.
- Refreshed HEAD showed intervening commit `a5a4acaa70ecaf93c61c09606002ff65e76f7169` claimed Formula evaluator signed-zero scope only and was disjoint.
- Claim commit: `1ee73b982a80ce21cc8ec962129dfa414b02fe41`.
- Production fix: `7667fad6ede3539e34af8f4ff63cd162c33604cd` — `fix(domain): canonicalize Opening sill-offset signed zero`.
- Focused regression: `68398d6a1e5f86bdd0b54ad97af077c4701817d5` — `test(domain): guard Opening sill-offset signed zero`.
- Post-regression refresh showed `main` exactly at `68398d6a1e5f86bdd0b54ad97af077c4701817d5`; no concurrent commit touched the reserved Opening source/test before closeout.

## Validation actually executed

- Exact source readback confirmed blob `207b7bc0008e8f248182c0cc1bb7d53078e47636`; the production diff is the one zero-canonicalizing return in `RequireFinite()`.
- Exact regression readback confirmed blob `6ead445c4dd3635115b45191d3228dbf5c30ac36`; it uses `BitConverter.DoubleToInt64Bits` for the signed-zero assertion and includes nonzero/strict-positive/non-finite sanity cases.
- Smoke-project csproj readback confirmed SDK default compile globs and direct Core project reference, so the new `.cs` module initializer is registered without project-file edits.
- Hosted environment has no `dotnet`, `csc`, `mcs` or `msbuild`, so managed compile/smoke execution is `NOT_RUN`; no managed PASS is claimed.
- No GitHub Actions, packaging, adapter build or licensed BricsCAD runtime qualification was dispatched/executed.

## Completion condition

Satisfied for this bounded Core source/static lane: `SillOffsetMm` stores/exposes canonical positive zero, legal negative nonzero offsets and strict-positive physical dimensions remain unchanged, exact source/test readback is complete, moving-main concurrency was reconciled without overwrite, and unavailable managed/native gates remain explicitly unclaimed.
