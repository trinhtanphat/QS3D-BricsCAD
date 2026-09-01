# Targeted regeneration known-Count integrity

## Scope

This contract covers caller-provided target element IDs consumed by `RegenerationEngine.RegenerateDirtySubset`. It is deterministic Core model-lifecycle behavior and does not require licensed BricsCAD runtime evidence.

## Integrity contract

At admission, every Count channel exposed through `ICollection<string>`, `IReadOnlyCollection<string>`, or non-generic `ICollection` must agree and be non-negative. Pure streaming sources remain supported.

For counted sources, the admitted Count is rebound immediately before and after each caller-controlled `MoveNext()`, and immediately after `Current` is detached before the target ID is validated or retained. Known over-yield is rejected before an unexpected `Current`; terminal under-yield and final Count drift are rejected before the materialized target set is published.

The existing project-element-count ceiling, canonical target identity validation, case-insensitive duplicate rejection, project structural freshness checks, dependency validation, transactional rollback and deterministic project-order target resolution remain authoritative.

## Validation

Run the auto-discovered `scripts/preflight-regeneration-target-known-count-integrity.py` guard and the Core smoke suite. Regression coverage must include transient Count drift caused by both `MoveNext` and `Current`, known over/under-yield, stable multi-interface counted input and a pure streaming control.

Hosted source/Core validation is authoritative for this package. Do not claim licensed BricsCAD/private-DWG `LOCAL_PASS` from this runbook.