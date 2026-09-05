# V25 BLT legacy element mutation lifecycle

## Scope

This source package closes a persistence/lifecycle gap in `QS3DBLTIMPORT`. Legacy BLT evidence is still derived by the existing clean-room adapter; this package does not change category inference, concrete/formwork formulas, proxy geometry interpretation, source Handle ownership, or native mutation semantics.

## Source contract

`BltLegacyCommands.ApplyLegacyEvidence` must publish persisted evidence through `ProjectElement.SetProperty` instead of writing the public `Properties` dictionary directly. When a candidate has no exact legacy formwork evidence, stale `FormworkM2` is removed through `ProjectElementQuantityLifecycleExtensions.RemoveQuantity`, which validates the quantity key and marks `ElementDirtyFlags.Quantity` only when an existing quantity was actually removed.

The absence of exact formwork remains explicit: the numeric quantity is absent and `CAD.BLT.FormworkStatus=PENDING_EXACT_EVIDENCE`. No default or inferred formwork quantity is fabricated.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-blt-legacy-element-lifecycle.py
```

Protected Shared CI must also complete exact-head `preflight` and `core`, including deterministic Core smoke and locked-reference BricsCAD V25 plugin compilation.

## LOCAL_ONLY qualification

Hosted source/static/V25 compile evidence is not proprietary BLT3D or licensed BricsCAD runtime PASS. A local licensed qualification agent should fetch the exact merged SHA, load the prepared V25 build, import representative legacy entities containing concrete/formwork evidence and entities lacking exact formwork evidence, then verify:

1. source CAD Handles remain unchanged;
2. persisted BLT property values are unchanged semantically from the pre-fix adapter output;
3. an element re-imported without exact formwork no longer retains stale `FormworkM2`;
4. `CAD.BLT.FormworkStatus` remains `PENDING_EXACT_EVIDENCE` in that case;
5. generated-output stale/dirty behavior is consistent with ordinary semantic property mutation;
6. save/cold-reopen preserves the lifecycle-updated element state.

Record licensed runtime evidence separately; do not promote hosted CI to `LOCAL_PASS`.
