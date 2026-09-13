# CostX Live Workbook dependency aggregation precision

`LiveWorkbookRefreshEngine2` recalculates workbook/BOQ bindings in deterministic dependency order. Upstream values are now accumulated with compensated summation before multiplier and offset application so finite high-dynamic-range cascades do not silently discard smaller quantities.

## Contract

- Dependency traversal and trace emission remain ordinal-ignore-case deterministic.
- Fresh/stale/refreshed/error/conflict semantics are unchanged.
- The public DTO and binding constructors are unchanged; this is backward-compatible for existing callers.
- Non-finite dependency aggregation fails closed as `LiveWorkbookFreshness.Error` and preserves the binding's last accepted value.
- Final source-plus-dependency, multiplier and offset arithmetic still uses the existing non-finite guard.
- Signed zero is canonicalized to positive zero for stable downstream serialization/comparison.

This closes a parity gap where a workbook total such as `1e16 + 1 + 1` could previously refresh to `1e16` despite all upstream values being finite and individually valid. The new behavior preserves the two smaller units and produces the deterministic representable result `10000000000000002`.

## Compatibility

No workbook IDs, BOQ line IDs, BIM/drawing source linkage, revision semantics, trace format, API routes, scopes or refresh response shapes change. Consumers only observe more numerically stable values for hostile mixed-magnitude dependency cascades.
