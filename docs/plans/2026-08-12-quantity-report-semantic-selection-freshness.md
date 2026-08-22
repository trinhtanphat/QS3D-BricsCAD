# Plan — Quantity report semantic selection freshness

## Problem

`ProjectQuantityReportBuilder.ResolveSelection(...)` enumerates caller-controlled lazy ids. Existing protection detects structural remove/replace of selected element instances after enumeration, but it does not detect semantic project changes recorded by `ProjectState.ChangeVersion` when the selected instances themselves remain stable.

## Contract

1. Capture `project.ChangeVersion` immediately before enumerating `elementIds`.
2. Materialize/validate the requested ids exactly as today.
3. Immediately after enumeration, compare the captured version with the current project version and fail closed on mismatch.
4. Keep `ReportingProjectIdentityGuard.RequireUniqueElementIds(...)` plus the existing selected-instance identity recheck unchanged; these protect direct structural mutations that do not necessarily call `Touch()`.
5. Ensure semantic mutation is rejected even when the lazy enumerable yields zero ids.
6. Preserve stable Group/Detail output and public API signatures.

## Regression

Focused Core smoke:
- stable lazy selection still builds expected Group and Detail reports;
- lazy selection that calls `project.Touch()` and then yields a valid selected id fails closed;
- lazy selection that calls `project.Touch()` and yields no ids fails closed before returning an empty report;
- structural protection remains present via static preflight tokens rather than duplicating the prior structural smoke lane.

## Validation

Add a focused static Python preflight that checks capture-before-foreach, version-check-after-foreach, and preservation of the existing structural instance freshness logic. Connector readback confirms committed content only; no runtime/build PASS is claimed unless actually executed.
