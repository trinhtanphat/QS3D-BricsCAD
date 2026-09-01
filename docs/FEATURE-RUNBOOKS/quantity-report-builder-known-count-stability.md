# QuantityReportBuilder known-Count stability

## Scope

This REMOTE_SAFE/Core-only contract covers `QuantityReportBuilder.Group(IEnumerable<ElementInstance>)`. It does not require or establish licensed BricsCAD runtime evidence.

## Integrity contract

When the caller exposes a supported known Count through `ICollection<ElementInstance>`, `IReadOnlyCollection<ElementInstance>`, or non-generic `ICollection`, grouping binds that evidence at admission and requires it to remain stable throughout traversal. Multiple supported Count surfaces must agree and no Count may be negative.

The caller-controlled traversal is intentionally explicit rather than `foreach` so validation occurs in this order:

`Count rebound -> MoveNext -> Count rebound -> known-count overrun admission -> Current`

This prevents an N+1 source or transient Count mutation from exposing a semantic `Current` value before the count-integrity contract rejects the source. A final rebound and exact observed-count comparison preserve under-yield detection.

Pure streaming `IEnumerable<ElementInstance>` inputs with no supported Count surface remain valid and retain historical grouping behavior.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/QuantityReportBuilderKnownCountStabilitySmoke.cs` covers:

- Count=1 with two yielded elements, proving only the first `Current` is read;
- transient Count growth after `MoveNext`, rejected before `Current`;
- transient Count shrink after `MoveNext`, rejected before `Current`;
- transient negative Count after `MoveNext`, rejected before `Current`;
- cross-interface Count conflict after `MoveNext`, rejected before `Current`;
- stable counted-source grouping/totals behavior;
- pure streaming-source compatibility.

`scripts/preflight-quantity-report-builder-known-count-stability.py` pins the production ordering and the hostile regression cases without replacing runtime-independent Core smoke validation.

## Validation

Run the feature guard and deterministic Core smoke through the repository's normal shared CI path. Exact-head branch CI is required before protected PR merge. Runtime status for this lane is `NOT_APPLICABLE`.
