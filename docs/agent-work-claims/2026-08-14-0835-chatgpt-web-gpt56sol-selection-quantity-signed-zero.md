# Work claim — Semantic Selection quantity signed-zero projection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-quantity-signed-zero-20260814-0835`
- Registered: `2026-08-14T08:35:00+07:00`
- Baseline main SHA: `966861a0cc507b55520093f772edaea23e9decb4`
- Priority: `P1 Core semantic-integrity hardening` — public semantic selection quantity projections must not leak IEEE negative-zero representation

## Confirmed source gap

`ProjectElement.SetQuantity()` now canonicalizes exact zero to positive `0d`, and the completed setter claim explicitly leaves direct writes through the public `Quantities` dictionary for downstream defensive boundaries. Quantity Report and MAP projections already canonicalize signed zero. `SemanticSelectionInspector.InspectQuantities(...)` rejects NaN/Infinity but copies a finite zero directly into `SemanticSelectionQuantityValue.Value`; a bypassed/direct `-0d` therefore leaks negative-zero bits through the selection projection. Because numeric equality considers `-0d` and `+0d` equal, a mixed selection can also expose whichever zero sign belongs to the first sorted element rather than a canonical value.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — quantity projection canonicality only
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs` — focused regression only
- this claim file

## Acceptance

1. Canonicalize finite exact-zero quantity values to positive `0d` while materializing selection quantity inspection.
2. A single directly injected `-0d` quantity must project as numeric zero with positive-zero bits.
3. `-0d` and `+0d` across selected elements remain non-mixed and project canonical positive zero independent of element ordering.
4. Preserve non-finite fail-closed behavior, missing/present counts, ordinary mixed numeric detection, quantity names/order and all property/reference inspection semantics.
5. Do not mutate project quantities while inspecting.

## Evidence / history

- `b0d55331bca2c7bff4d0709407eac8063443bb3d` completed canonical `ProjectElement.SetQuantity()` signed-zero handling and explicitly kept direct dictionary writes in scope for downstream defensive projections.
- `5426f0a801ad0f51d288a79391467b688e471f8d` canonicalized signed zero at the public Quantity Report projection boundary.
- Current `SemanticSelectionInspector.InspectQuantities(...)` at the baseline checks only NaN/Infinity before storing the raw `double` in the result.
- Current focused inspector smoke has no signed-zero projection coverage; targeted commit search found no existing semantic-selection signed-zero claim/fix.

## Explicit non-scope

No changes to `ProjectElement.SetQuantity`, quantity arithmetic, reports/MAP, measurement, persistence, cost, IFC, recognition, properties/references, UI or BricsCAD/native adapters. No GitHub Actions dispatch; no force-push.

## Validation plan

Publish this claim alone, refresh live `main`, recheck overlap for the two reserved files, apply the one-boundary canonicalization plus focused smoke, re-fetch exact diffs/current source, then close `COMPLETED`. Managed/native execution remains `NOT_RUN` unless an actual executable path is available.
