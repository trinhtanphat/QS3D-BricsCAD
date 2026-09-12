# V25 Quantity visible-render lifecycle

## Scope
This runbook covers the source-safe Quantity Insight / QS3DBQ lifecycle work owned by issue #6285. It does not convert accessibility/live-row evidence into a visible-render PASS and does not claim that the event-ownership defect is the proven cause of the licensed blank/grey/white raster symptom.

## Native document-event ownership
`QuantityInsightPanel.DetailExplainer.ExactFace` and `QuantityInsightPanel.RaftHighlight` each own `DocumentToBeDeactivated` and `DocumentBecameCurrent` independently.

For each handler:
- publish the ownership bit immediately after a successful add accessor;
- never infer ownership of one handler from success of the other;
- on detach, attempt each owned remove independently;
- clear an ownership bit only after that remove succeeds;
- if a recoverable remove fails, retain the bit so a later unload can retry and a later load cannot double-subscribe.

The focused source guard is `scripts/preflight-v25-quantity-visible-render-lifecycle.py`.

## Validation boundary
REMOTE_SAFE evidence includes the source guard, diff checks, admitted-reference compilation and protected Shared/Hybrid CI. Licensed BricsCAD validation remains LOCAL_ONLY for modeless/palette lifecycle, MDI switching, native document event behavior, theme/resource presentation and actual raster rendering.
