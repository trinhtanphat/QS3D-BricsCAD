# Work claim — BulkEdit numeric underflow integrity

- Status: `ACTIVE`
- Agent: `ChatGPT web / GPT-5.6 Sol`
- Registered: `2026-08-12T15:55:00+07:00`
- Baseline main SHA: `41c2c5c7217986995151db282216bd9b4b18f09a`
- Priority: Proven Core bulk-edit numeric integrity defect: direct invariant parsing and multiplication can silently collapse mathematically non-zero semantic property values to exact zero.

## Reserved scope

Harden `BulkEditService.MultiplyNumericProperty()` so it fails closed when:

- a syntactically non-zero invariant numeric property token parses to exact zero because of IEEE-754 underflow; or
- finite non-zero parsed value and finite non-zero factor multiply to exact zero.

Preserve legitimate zero tokens/operands, representable subnormal results, exact numeric no-op behavior, existing overflow handling, atomicity, project freshness and generated-geometry dirty semantics.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- focused `tests/QS3D.Core.SmokeTests/` regression coverage for BulkEdit numeric underflow
- this claim file

## Excluded scope

- `SemanticNumber.Get()` regeneration parsing (already completed independently).
- `QuantityMath` arithmetic (already completed independently).
- Formula parsing, measured-solid parsing, Curtain/Grid/geometry arithmetic, persistence, native CAD/runtime, release/workflow changes.

## Counterexamples

- Stored property token `"1e-4000"` with factor `2` currently parses to `0d`; the exact-numeric no-op guard then silently accepts a mathematically non-zero property as zero.
- Stored property token `"5e-324"` with factor `0.5` currently parses to `double.Epsilon` but multiplication underflows to `0d`, allowing BulkEdit to publish false zero.

## Validation plan

- Reject non-zero numeric-token parse underflow before any mutation.
- Reject non-zero multiplication underflow before any mutation.
- Preserve exact-zero scientific tokens and true-zero multiplication.
- Preserve representable subnormal results and existing x1 lexical/freshness no-op behavior.
- Verify failure is all-or-nothing for project/property/freshness state.
- No GitHub Actions or BricsCAD runtime PASS will be claimed unless separately executed under repository policy.

## Coordination

Recent BulkEdit numeric no-op work changed the same method but addressed lexical x1 no-ops, not underflow. Current claim/history search found no ACTIVE neighboring claim owning this underflow scope. Re-check concurrent claims and `main` immediately before implementation writes.

## Completion condition

Product guard and focused regression are pushed to `main`; source/test readback confirms the exact intended behavior; this claim is then closed `COMPLETED` with exact commit evidence and no overlapping ACTIVE ownership left unresolved.
