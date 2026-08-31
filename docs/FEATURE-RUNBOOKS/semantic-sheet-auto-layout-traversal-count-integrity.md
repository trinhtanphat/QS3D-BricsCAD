# Semantic Sheet Auto Layout traversal Count integrity

## Scope

This contract covers the two caller-controlled enumerations used by `SemanticSheetAutoLayoutPlanner.Build`: requested auto-layout items and the available semantic-view catalog. It is deterministic Core behavior and does not require licensed BricsCAD runtime evidence.

## Integrity contract

At admission, every Count channel exposed through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection` must agree, be non-negative, and remain within the 10,000-item ceiling. Pure streaming sources with no known Count remain supported.

For a counted source, the admitted Count is rebound immediately before and after each caller-controlled `MoveNext()` and immediately after `Current` is detached, before the returned item/view is retained. A transient Count change is therefore rejected even when the source restores its original Count before the next loop edge or final publication.

The existing no-overread ceiling, final traversal-cardinality equality, null/duplicate/missing-view validation, deterministic ordering/packing, sheet placement cap, option validation, and floating-point precision guards remain authoritative.

## Validation

Run the auto-discovered `scripts/preflight-semantic-sheet-auto-layout-traversal-count-integrity.py` guard and the Core smoke suite. Hostile regression coverage must exercise Count drift caused by both `MoveNext` and `Current` for requested items and available views, while a stable multi-interface counted source remains accepted.

Hosted source/Core validation is authoritative for this package. Do not report licensed BricsCAD `LOCAL_PASS` from this runbook.