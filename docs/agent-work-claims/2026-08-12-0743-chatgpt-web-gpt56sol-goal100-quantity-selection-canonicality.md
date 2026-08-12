# Work claim — Goal-100 backlog synthesis and quantity selection canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-goal100-quantity-selection-20260812-0743`
- Registered: `2026-08-12T07:43:25+07:00`
- Baseline main SHA: `c3849ed39c3999c91aade89e65547c51657a34bd`
- Priority: Owner-requested detailed re-audit, canonical remaining-work note, and evidence-driven remote-safe bug hardening toward 100% completion.

## Reserved scope

1. Add one repository-level Markdown snapshot that consolidates the currently unfinished work needed for a defensible 100% product claim, classified as remote-safe, `LOCAL_ONLY`, `POLICY_REQUIRED`, `ENGINEERING_REQUIRED`, or `FORMAT_SCOPE_REQUIRED`. The snapshot is an index/synthesis only; current source, issues, product boundary and `docs/LOCAL-AGENT-INBOX.md` remain authoritative.
2. Harden `ProjectQuantityReportBuilder.Group/Detail(..., IEnumerable<string> elementIds)` so duplicate caller selection IDs are rejected as non-canonical instead of silently normalized. This closes the demonstrated liveness hole where a lazy enumerable can repeat one valid ID indefinitely and keep `ResolveSelection()` enumerating forever.
3. Add focused deterministic Core smoke coverage for canonical unique selection, duplicate-ID refusal (including case-only duplicate identity), unknown/blank refusal preservation, and early termination before a duplicate lazy sequence can continue indefinitely.

## Expected surfaces

- `docs/GOAL-100-REMAINING-2026-08-12.md` (new)
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportSelectionCanonicalitySmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file for completion close-out

## Excluded scope

- No changes to legacy `QuantityReportBuilder` grouping, quantity math, XLSX exporters/readers, BQ locate UI, Room Finish grouping, or currently claimed reporting lanes.
- No BricsCAD V25/V26 runtime execution, NETLOAD/DemandLoad, private-DWG, native geometry/UI, performance, installer or signing qualification.
- No fabrication/code-compliance values, commercial licensing policy, IFC/Revit/BCF/vendor format implementation, or invented native ownership semantics.
- No GitHub Actions dispatch or release publication.
- No status change to existing open product-gap issues or `LOCAL-*` gates from remote evidence.

## Validation plan

- Re-fetch `main` and relevant claim/commit history before implementation and again before integration.
- Re-read the exact current `ResolveSelection()` implementation and preserve blank/unknown-ID fail-closed behavior.
- Add focused smoke that proves duplicate identity is rejected case-insensitively and that a lazy duplicate source is stopped on the second item rather than continuing enumeration.
- Register the smoke in the aggregate Core smoke harness.
- Review the final diff against the latest `main`; do not claim .NET/BricsCAD runtime PASS unless actually executed in an authorized environment.

## Coordination

Recent current-main claim/commit searches show active work around browser bounds/reference canonicality, health null handling, Grid identity, XLSX integrity, audit integrity and other quantity/reporting defects, but no current claim for `ProjectQuantityReportBuilder` selection-ID duplicate/lazy-enumeration canonicality or a `GOAL-100-REMAINING` synthesis. Existing project-quantity group-key collision work is a neighboring completed/independent lane and is explicitly not modified here.

## Completion condition

Completed only when the consolidated remaining-work Markdown is present, project-quantity selection IDs fail closed on duplicates without weakening blank/unknown checks, focused regression source is registered, the implementation remains on current `main` after concurrent integration, and this claim is updated to `COMPLETED` with the exact pushed commit evidence and remaining qualification boundary.
