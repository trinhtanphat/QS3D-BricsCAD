# Project diagnostic summary known-Count stability

## Scope

`ProjectDiagnosticSummaryExporter.MaterializeIssues` accepts both counted collections and pure streaming `IEnumerable<ModelHealthIssue>` sources. Counted sources are caller-controlled evidence and must remain stable for the full traversal before their issues can be retained for summary publication.

## Contract

- Admission validates every applicable `ICollection<ModelHealthIssue>`, `IReadOnlyCollection<ModelHealthIssue>` and non-generic `ICollection` Count surface.
- Negative, over-limit or conflicting Count evidence fails before enumeration.
- For an admitted known Count, the exporter rebinds Count immediately before and after each `MoveNext`, and immediately after each semantic `Current` read.
- Count drift fails before retaining the current item.
- A stable counted source cannot over-yield: the first item beyond the admitted Count is rejected before reading that item's `Current`.
- A stable counted source cannot under-yield: traversal completion must match the admitted Count exactly.
- The independent `MaxIssueCount` streaming ceiling remains in force for sources with no known Count surface.
- Pure streaming sources remain one-pass and supported.
- Existing null/severity validation, grouping, canonical code ordering, JSON shape and export behavior are unchanged.

## Deterministic evidence

`tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummaryKnownCountStabilitySmoke.cs` covers MoveNext-induced drift, Current-induced drift, stable known-Count over-yield and under-yield, plus honest counted and pure streaming controls.

`scripts/preflight-project-diagnostic-summary-known-count-stability.py` pins the required production ordering and regression cases so a future refactor cannot silently return to admission-only Count validation.

## Runtime classification

`NOT_APPLICABLE`: this package is deterministic Core diagnostics/reporting correctness and requires no licensed BricsCAD runtime or private DWG evidence.
