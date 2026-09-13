# CostX Live Workbook equivalent-source tie-break determinism

## Scope

`LiveWorkbookRefreshEngine2` keeps the existing case-insensitive compatibility contract for source identity, revision comparison, evidence conflict detection, stale detection, and current-revision checks. This change does not introduce a new DTO, route, constructor, freshness state, conflict rule, or workbook identity rule.

## Problem closed

Equivalent source snapshots can differ only by casing while remaining the same logical source under the existing compatibility rules. The previous candidate ordering used only `StringComparer.OrdinalIgnoreCase` for revision and evidence. Equal ignore-case keys therefore inherited source enumeration order from LINQ's stable sort, allowing the published `ResolvedRevision`, `SourceEvidenceReference`, and source trace casing to vary when the same source set arrived in a different order.

That was a deterministic recalculation and traceability gap even though the numeric quantity was unchanged.

## Contract

Candidate selection now preserves the existing ignore-case ordering first, then applies explicit ordinal tie-breaks for revision and evidence. Therefore:

- the same logical source set produces the same selected snapshot regardless of input enumeration order;
- revision/evidence casing in refresh results and trace output is stable;
- equivalent case-only snapshots remain non-conflicting, preserving backward compatibility;
- genuinely different fingerprints still fail closed as `Conflict`;
- BIM-element and drawing-handle lookup, stale/fresh propagation, dependency cascades, compensated aggregation, and last-accepted-value failure behavior are unchanged.

## Regression evidence

`QsLiveWorkbookSourceTieBreakSmoke` is auto-executed with `ModuleInitializer`. It refreshes the same case-equivalent source set in forward and reverse order and requires identical freshness, resolved revision, evidence, trace, and value, while checking the canonical ordinal tie-break result.

Hosted smoke remains source-safe evidence only; licensed BricsCAD/desktop UX qualification is a separate boundary.
