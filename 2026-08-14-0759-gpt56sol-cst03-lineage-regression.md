# Work claim — CST-03 measurement-lineage regression completion

- Status: `COMPLETED`
- Agent: `gpt56sol-cst03-lineage-regression-20260814-0759`
- Registered: `2026-08-14T07:59:00+07:00`
- Completed: `2026-08-14T08:05:00+07:00`
- Baseline main SHA: `192c7e794ff0e998f8e49e010fc8e1beea72fe5b`
- Priority: `P1` CST-03 revision cost integrity; closes a verified regression gap left by a prior `RELEASED` claim.

## Reserved scope

Complete deterministic regression coverage for the already-published `EstimateRevisionCostImpact` measurement-lineage guard. Verify that revision cost comparison fails closed when the exact measurement trace identity tuple changes, while preserving existing comparable quantity/rate/commercial-adjustment behavior.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/EstimateRevisionCostImpactSmoke.cs` — focused regression implementation.
- `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs` — read/verify current guard only; no source rewrite required.
- this claim file.

## Excluded scope

- `EstimateLine.cs`, RateBook/rate resolution, mapping/BOQ, persistence, report/UI/native behavior.
- MeasurementTrace/MTR-05 semantics.
- V25 preview packaging/version automation and any current release workflow surfaces.
- GitHub Actions dispatch and BricsCAD native qualification.

## Implementation

- Claim-only commit: `ebeab0b2f0212783721b73d6798d56994010731f`.
- Existing production guard retained from `5a99e10e21e8975f15ae48b5ff979082ac49ba01`: `EstimateRevisionCostImpact.RequireComparable()` compares `SemanticIdentity`, `SourceIdentity`, and `QuantityKey` with exact ordinal semantics before cost-delta arithmetic.
- Regression commit: `e2171f4cf63d5502fb31e8a30644d6c9739100e3`.
- Added `MeasurementIdentityScopeIsStrict()` covering semantic identity mismatch (including case-only `SEM` vs `sem`), source identity mismatch, and quantity-key mismatch.
- Existing quantity-only, rate-only, combined, commercial-adjustment, unchanged-state, comparability, and overflow regressions remain intact.

## Validation actually performed

- Refreshed current `main` immediately after the claim and before the test write; concurrent commits were confined to V25 release automation and did not overlap the Cost surfaces.
- GitHub readback on later current `main` confirmed the three lineage cases are present in `EstimateRevisionCostImpactSmoke.cs` and the production lineage guard remains present.
- Commit comparison confirmed `e2171f4cf63d5502fb31e8a30644d6c9739100e3` is an ancestor of later `main`; subsequent observed commits touched release automation, Curtain/local coordination, Rebar stock demand, and smoke registration rather than the CST-03 source/test surface.
- Local managed build/smoke was **not executed**: this container has no `dotnet`, `csc`, `mcs`, `msbuild`, `xbuild`, or `mono`, and outbound DNS prevents cloning GitHub into the container. No managed PASS is claimed from this session.
- No GitHub Actions were dispatched.
- No BricsCAD native runtime test was executed or claimed.

## Coordination

The earlier claim `2026-08-13-2345-gpt56sol-cst03-measurement-lineage-integrity.md` remains `RELEASED`; this follow-up closes only its missing regression/evidence-on-main gap. The V25 release-automation lane remains separately owned and non-overlapping.

## Remaining gates

- Managed full Core build/smoke should run in an environment with the repository checkout and .NET SDK/toolchain before using this lane as fresh executable evidence for a release gate.
- BricsCAD native qualification is not required for this pure Core cost invariant and was not attempted.

## Completion condition

Satisfied for repository implementation/coverage: focused measurement-lineage regression is pushed and read back from current `main`, the existing source guard remains present, actual validation limitations are recorded without fabricated PASS claims, and this claim is closed `COMPLETED`.