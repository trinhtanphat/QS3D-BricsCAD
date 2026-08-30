# Deep Cost Current-induced Count stability

## Purpose

Deep Cost and BQ workflows accept caller-controlled `IEnumerable<T>` inputs while preserving deterministic bounds, canonical ordering and fail-closed commercial truth. When an input exposes a supported known Count, that admitted Count is part of the traversal contract for the complete read.

## Contract

For the five caller-controlled traversals in `DeepCostWorkflows.cs` — rate-reference edges, build-up rates, trade-analysis items, BQ library entries and BQ project-import entries — known Count is rebound before `MoveNext`, after each successful `MoveNext`, **after Current**, and after traversal.

The post-Current rebound occurs before null, identity, grouping, reference, snapshot, or import acceptance. A hostile `Current` getter therefore cannot change admitted cardinality and have the returned item processed before the Count violation is observed.

Existing overrun-before-Current ordering, maximum-entry ceilings, under-yield checks, duplicate/null rejection, deterministic sorting/grouping and final Count validation remain authoritative. Inputs with no supported Count surface remain pure streaming inputs and retain their existing bounded traversal behavior.

## Deterministic regression

`DeepCostCurrentCountStabilitySmoke` uses counted sources whose `Current` access induces Count drift while returning a null item. The required behavior is to report the Count-stability violation before the ordinary null-item acceptance guard. Stable counted controls remain accepted.

`scripts/preflight-deep-cost-current-count-stability.py` pins the post-Current Count rebound for all five production loops and the focused smoke/runbook contract.

This is deterministic Core/commercial input-integrity work and requires no licensed BricsCAD runtime or private DWG evidence.
