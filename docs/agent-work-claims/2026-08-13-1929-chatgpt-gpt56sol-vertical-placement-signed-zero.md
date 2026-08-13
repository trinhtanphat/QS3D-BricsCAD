# Work claim — vertical placement signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-vertical-placement-signed-zero-20260813`
- Registered: `2026-08-13T19:29:00+07:00`
- Completed: `2026-08-13T19:33:00+07:00`
- Baseline main SHA: `0e128fe5ad1eefd46e8aaa951073a915f114d3d0`

## Confirmed defect

`ElementVerticalPlacement` validated finite bottom/top elevations but stored raw zero representations; `HostedOpeningVerticalPlacement` accepted non-negative `relativeSillM` and stored raw `-0d`; `OptionalFiniteProperty`/`ReadLevelOffset` parsed and returned raw `-0d`. These are already validated semantic numeric states where zero is valid, so equivalent zero values could retain non-canonical IEEE-754 sign bits.

## Implemented scope

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementSignedZeroSmoke.cs`
- this claim file

## Implemented change

Accepted finite zero is canonicalized to literal `+0d` at `ElementVerticalPlacement` bottom/top boundaries, `HostedOpeningVerticalPlacement.RelativeSillM`, parsed level-offset output and finite addition results. Existing finite/non-finite, positive-height, level-reference, hosting, tolerance and overflow/fail-closed behavior is unchanged.

Focused `[ModuleInitializer]` smoke bit-checks direct bottom/top zero, hosted relative sill, parsed `BottomLevelOffsetM`, legacy `Resolve()` zero arithmetic, and retained invalid/non-finite refusal.

## Coordination / moving-main reconciliation

Exact recent searches for `vertical placement signed zero` and `ElementVerticalPlacement signed-zero` returned no competing lane before claim.

- claim: `804f3e63801ba492cb08a7ba705ac7e308078f60`
- source: `4a0e28a7b5cdde46e9d763fa7ea68b58ef8de0b7`
- regression: `eb1cdaa7e4d0629f966c96c772d27a3e3a17a6a5`
- source readback blob: `7b824eaf40fcadcb46981402a222f31441da91b6`
- smoke readback blob: `345441db8dcd47654532133d976d173e75983df5`

Concurrent ModelHealth and Formula commits were disjoint; Formula completed at `79f3650122ab3152382282ff62c16059ba932335` immediately before this closeout. A new ModelHealth numeric-source-identity claim is disjoint and remains owned by its agent.

## Validation actually performed

Exact GitHub source/test readback and moving-main reconciliation only. No managed build/smoke process, GitHub Actions, adapter build, package, or licensed BricsCAD runtime was executed in this connector-only lane; no execution PASS is claimed.

## Completion condition

Satisfied for this bounded Core source/static lane: accepted zero-valued vertical-placement state is canonical positive zero, legacy invalid/non-finite semantics remain guarded, focused registered regression is on current `main`, exact readback is verified, and unavailable execution gates remain explicitly unclaimed.
