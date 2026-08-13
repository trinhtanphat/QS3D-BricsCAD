# Work claim — ProjectElement quantity signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-quantity-signed-zero-20260813-1831`
- Registered UTC: `2026-08-13T11:31:00Z`
- Baseline main SHA: `1391ef8275e00e652cdd4e1cfd9287f00269c387`
- Priority: `MTR-05 / P0 continuous hardening` — canonical quantity setter must not persist IEEE negative zero

## Confirmed defect

`ProjectElement.SetQuantity()` rejects non-finite values but stores every finite value unchanged. Explicit IEEE `-0.0` is therefore retained in the canonical semantic quantity dictionary. Because `double.Equals(-0d, 0d)` is true, a later `SetQuantity(..., +0.0)` call is treated as an identical-value no-op and cannot replace the negative-zero representation.

The repository already canonicalizes signed zero at UnitScale, public Quantity Report and MAP coverage projection boundaries. Keeping `-0.0` in the canonical setter creates an avoidable source-level representation split and forces downstream projections to defend it repeatedly.

## Reserved files

- `src/QS3D.Core/Domain/ProjectElement.cs` — `SetQuantity()` only
- `tests/QS3D.Core.SmokeTests/ProjectElementSetQuantityDirtySmoke.cs` — focused setter regression only
- this claim file

## Scope

- Canonicalize incoming exact-zero finite values to positive `0d` before the existing equality/no-op check and dictionary write.
- Add focused smoke coverage proving `SetQuantity()` stores positive-zero bits for explicit negative-zero input and a subsequent positive-zero write remains a no-op.
- Preserve current key trimming, NaN/Infinity rejection, quantity dirty propagation, timestamp semantics for actual changes and generated-geometry non-staleness.
- Do not attempt to police direct writes through the public `Quantities` dictionary; MAP/report projection guards remain valid corruption/defense boundaries.
- Do not change QSDB/persistence, MeasurementTrace/active none-reconciliation, Mapping, reports/UI, regeneration algorithms, rates/cost, geometry or BricsCAD/native surfaces.

## Initial overlap check

- Historical ProjectElement `SetQuantity` dirty-propagation lane is `COMPLETED`; no active reservation remains on this setter/test.
- Current `MTR-05 none trace reconciliation` reserves only `MeasurementTrace.cs` and its contract smoke; no overlap.
- REV-03A and current UI/native/docs claims reserve unrelated surfaces.
- Targeted current history found no `SetQuantity` signed-zero claim/fix.

## Validation plan

- Re-fetch current `main` after claim publication and recheck overlap before source mutation.
- Keep source diff to one exact-zero normalization line in `SetQuantity()`.
- Extend the existing auto-registered SetQuantity dirty smoke; assert numeric zero, positive-zero sign bits and no-op dirty/timestamp behavior after canonical storage.
- Re-fetch exact source/test after push and reconcile current `main` before closeout.
- No GitHub Actions and no `.NET`/native PASS without actual execution.

## Completion condition

`ProjectElement.SetQuantity()` stores canonical positive zero for explicit negative-zero input, focused setter regression is on current `main`, existing dirty/no-op semantics remain intact, concurrent work is reconciled without force-push/overwrite, and this claim is closed `COMPLETED` with truthful validation status.
