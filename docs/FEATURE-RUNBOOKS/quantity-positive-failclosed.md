# Quantity positive-input fail-closed contract

Issue: #4378  
Lane-Key: `issue-4378`  
Runtime: `NOT_APPLICABLE` — Core quantity correctness only.

## Defect boundary

Core quantity regeneration routes non-negative physical dimensions, areas and counts through `QuantityMath.Positive`. The historical implementation returned zero for any input that was not both finite and strictly positive. That made valid zero convenient, but also silently converted negative values, NaN and infinities into zero.

A concrete commercial/quantity consequence exists in both wall linked-opening paths: a clean Door/WallOpening reuses cached `OpeningAreaM2` through the same helper. Because `ProjectElement.Quantities` can be populated outside `SetQuantity` during deserialization/import/corruption scenarios, invalid cache state could be treated as a zero deduction and the host wall could publish gross-like net quantities instead of refusing corrupt authority.

## Required contract

`QuantityMath.Positive` is the shared non-negative normalization boundary for its current regeneration consumers:

1. finite positive input is preserved exactly;
2. `0d` and `-0d` are accepted and canonicalized to `+0d`;
3. negative finite input fails closed;
4. NaN and positive/negative infinity fail closed;
5. no invalid input may be silently converted to zero.

The helper remains intentionally separate from multiplication/addition/subtraction/division overflow and precision-loss checks.

## Wall cached-opening regression

`WallOpeningHostCanonicalitySmoke` locks the user-visible authority boundary for both `WallRegenerator` and `StructuralRegenerator`:

- clean cached `OpeningAreaM2` values of NaN, ±Infinity or negative numbers must fail before host quantity publication;
- prior sentinel host quantities remain unchanged on failure;
- a dirty opening ignores its stale/corrupt cache and recomputes from valid WidthM × HeightM;
- canonical finite positive cached values continue to deduct normally;
- representative negative semantic dimensions fail closed rather than becoming zero.

`preflight-quantity-positive-failclosed.py` is auto-discovered by aggregate feature guards and prevents restoration of the former invalid-to-zero expression while retaining both linked-opening consumers on the hardened helper.

## Acceptance

Exact-head automatic branch CI and the current protected PR candidate must both report `preflight` and `core` SUCCESS. Merge uses the expected-head protected PR path, then exact `main` is refreshed and verified. No licensed BricsCAD runtime evidence is required or claimed.
