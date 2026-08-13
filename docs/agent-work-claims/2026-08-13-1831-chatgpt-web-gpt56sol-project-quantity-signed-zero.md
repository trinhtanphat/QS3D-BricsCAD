# Work claim — ProjectElement quantity signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-quantity-signed-zero-20260813-1831`
- Registered UTC: `2026-08-13T11:31:00Z`
- Last updated UTC: `2026-08-13T11:36:00Z`
- Baseline main SHA: `1391ef8275e00e652cdd4e1cfd9287f00269c387`
- Priority: `MTR-05 / P0 continuous hardening` — canonical quantity setter must not persist IEEE negative zero

## Confirmed defect

`ProjectElement.SetQuantity()` rejected non-finite values but stored every finite value unchanged. Explicit IEEE `-0.0` was therefore retained in the canonical semantic quantity dictionary. Because `double.Equals(-0d, 0d)` is true, a later `SetQuantity(..., +0.0)` call was treated as an identical-value no-op rather than changing representation.

The repository already canonicalizes signed zero at UnitScale, public Quantity Report and MAP coverage projection boundaries. Keeping `-0.0` in the canonical setter created an avoidable source-level representation split and forced downstream projections to defend it repeatedly.

## Reserved files

- `src/QS3D.Core/Domain/ProjectElement.cs` — `SetQuantity()` only
- `tests/QS3D.Core.SmokeTests/ProjectElementSetQuantityDirtySmoke.cs` — focused setter regression only
- this claim file

## Implemented scope

- `SetQuantity()` now canonicalizes incoming exact-zero finite values to positive `0d` before the existing equality/no-op check and dictionary write.
- Explicit negative-zero input through the canonical setter therefore stores positive-zero bits.
- Added focused regression proving numeric zero, positive-zero sign bits, normal initial Quantity dirty behavior, and no-op dirty/timestamp semantics for a subsequent `+0.0` write.
- Preserved current key trimming, NaN/Infinity rejection, quantity dirty propagation and generated-geometry non-staleness.
- Direct writes through the public `Quantities` dictionary remain outside this setter lane; downstream MAP/report defensive canonicality remains intentionally valid.
- Did not change QSDB/persistence, Mapping, reports/UI, regeneration algorithms, rates/cost, geometry or BricsCAD/native surfaces.

## Coordination / overlap reconciliation

- Claim-only commit: `ed1926109c5265c9bb81cb16a18ee51b18bc2010` — `chore(agent): claim ProjectElement quantity signed-zero canonicality`.
- Post-claim refresh showed the claim at HEAD before source mutation.
- Concurrent Platform migration docs and active MTR-05 none-trace work touched only their own docs/MeasurementTrace surfaces.
- The first full-file connector source write (`2467a4286aa8204e8fad88db15a85695eff6f13f`) correctly added the SetQuantity normalization but accidentally changed `GeneratedBeamStirrupHandlesKey` to a nonexistent singular token in one unrelated stale-query line. Exact commit-diff inspection caught this before regression/closeout.
- Corrective commit `105d8c01c9e6a2388a689ffab5081af67d141697` restored the Beam Stirrup line immediately. Cumulative compare from the claim commit through the corrected source showed the final net `ProjectElement.cs` diff is exactly one added normalization line.
- Final cumulative compare `ed1926109c5265c9bb81cb16a18ee51b18bc2010..76f51bab8178f2f346b2aa6015cf2f3eaf38dd5c` shows `ProjectElement.cs` with exactly `+1/-0` and the reserved setter smoke with `+27/-0`; other changed files belong to concurrent Platform/MTR agents.

## Implementation commits

- `2467a4286aa8204e8fad88db15a85695eff6f13f` — `fix(core): canonicalize SetQuantity signed zero` — included the intended one-line fix plus an accidental unrelated typo caught by diff review.
- `105d8c01c9e6a2388a689ffab5081af67d141697` — `fix(core): restore beam stirrup stale key` — immediate corrective commit; removes the accidental change completely from final cumulative source state.
- `76f51bab8178f2f346b2aa6015cf2f3eaf38dd5c` — `test(core): guard SetQuantity signed zero`.

## Validation actually executed

- Executed: current-`main` refresh before claim, post-claim ownership recheck, exact source commit diff inspection, immediate corrective diff inspection, source/test readback and cumulative compare reconciliation.
- Executed: direct current-`main` readback confirms `SetQuantity()` contains only the intended exact-zero normalization before the existing equality check.
- Executed: regression diff inspection confirms explicit negative-zero construction via sign bit, stored positive-zero bit assertion, initial Quantity dirty assertion and subsequent positive-zero no-op timestamp/dirty assertions.
- Executed: cumulative compare proves no net Beam Stirrup or other unrelated `ProjectElement` change remains after correction.
- Not executed: GitHub Actions, repository `.NET` build, Core smoke executable, BricsCAD V25/V26 build/runtime or licensed native qualification. No PASS is claimed for any unexecuted gate.
- Local `git` fallback was probed only after connector full-file limitations became relevant; this environment had no repository checkout, no `gh`, and DNS could not resolve `github.com`, so all remote writes remained connector-backed. No force-push was used.

## Completion

Completed for this bounded canonical setter lane: claim-first ownership was published, signed-zero input through `SetQuantity()` now stores positive-zero representation, focused regression source is on current `main`, the accidental full-file typo was caught and fully reverted before closeout, cumulative diff proves only intended final source/test changes remain, concurrent work was respected, and unexecuted managed/native gates remain explicitly unclaimed.
