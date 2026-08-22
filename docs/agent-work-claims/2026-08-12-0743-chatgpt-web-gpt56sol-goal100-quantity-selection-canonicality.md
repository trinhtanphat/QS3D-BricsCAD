# Work claim — Goal-100 backlog synthesis and quantity selection canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-goal100-quantity-selection-20260812-0743`
- Registered: `2026-08-12T07:43:25+07:00`
- Baseline main SHA: `c3849ed39c3999c91aade89e65547c51657a34bd`
- Priority: Owner-requested detailed re-audit, canonical remaining-work note, and evidence-driven remote-safe bug hardening toward 100% completion.

## Reserved scope

1. Add one repository-level Markdown snapshot that consolidates the currently unfinished work needed for a defensible 100% product claim, classified as remote-safe, `LOCAL_ONLY`, `POLICY_REQUIRED`, `ENGINEERING_REQUIRED`, or `FORMAT_SCOPE_REQUIRED`. The snapshot is an index/synthesis only; current source, issues, product boundary and `docs/LOCAL-AGENT-INBOX.md` remain authoritative.
2. Harden `ProjectQuantityReportBuilder.Group/Detail(..., IEnumerable<string> elementIds)` so duplicate caller selection IDs are rejected as non-canonical instead of silently normalized. This closes the demonstrated liveness hole where a lazy enumerable can repeat one valid ID indefinitely and keep `ResolveSelection()` enumerating forever.
3. Add focused deterministic Core smoke coverage for canonical unique selection, duplicate-ID refusal (including case-only duplicate identity), unknown/blank refusal preservation, and early termination before a duplicate lazy sequence can continue indefinitely.

## Implemented surfaces

- `docs/GOAL-100-REMAINING-2026-08-12.md` (new)
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportSelectionCanonicalitySmoke.cs` (new)
- this claim file for completion close-out

`tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` was intentionally not modified: the focused smoke follows the repository's existing self-registration pattern and registers through `[ModuleInitializer]` in its own file, avoiding a shared registration-file collision during heavy concurrent development.

## Excluded scope

- No changes to legacy `QuantityReportBuilder` grouping, quantity math, XLSX exporters/readers, BQ locate UI, Room Finish grouping, or concurrently claimed reporting lanes.
- No BricsCAD V25/V26 runtime execution, NETLOAD/DemandLoad, private-DWG, native geometry/UI, performance, installer or signing qualification.
- No fabrication/code-compliance values, commercial licensing policy, IFC/Revit/BCF/vendor format implementation, or invented native ownership semantics.
- No GitHub Actions dispatch or release publication.
- No status change to existing open product-gap issues or `LOCAL-*` gates from remote evidence.

## Integration evidence

- Claim registration: `c64fe9222cf12583084b26a9be7f0807b7bedc5f`
- Source fix: `d5c5d1a49bd704db8f16ceaf0eceaf10f6964e0a`
- Focused regression: `f75c1cd335b9dee0d29ca0ba6af667464e7ac693`
- Goal-100 remaining-work synthesis: `a767ae5e2f6838f4f5e86b5c937e681b2a0b6417`

Because `main` was receiving multiple agent commits per second, the implementation was integrated through GitHub's path-scoped Contents API after repeated parent-locked fast-forward attempts correctly refused stale parents. No force update was used. Direct readback from current `main` confirms the source fix, regression file and goal-100 Markdown remain present after concurrent integration.

## Validation implemented

- `ResolveSelection()` now trims each caller ID and requires case-insensitive uniqueness; a duplicate throws `ArgumentException` immediately instead of being silently ignored.
- Blank selection IDs remain `ArgumentException`; unknown IDs remain `KeyNotFoundException`.
- Focused smoke covers valid Group/Detail selection, exact duplicate refusal, case/whitespace duplicate refusal, preservation of blank/unknown refusal, and a lazy duplicate enumerable that throws if enumeration continues past the duplicate.
- The smoke self-registers via `[ModuleInitializer]`, matching an existing Core smoke registration pattern.
- `docs/GOAL-100-REMAINING-2026-08-12.md` records the remaining release/native/policy/engineering/format gates and a concrete 100% closure checklist.
- Source, smoke and Markdown were re-read directly from current `main` after integration.

## Validation boundary

Remote source/static review only for this lane. No .NET build/test process was executed by this session, no GitHub Actions workflow was dispatched, and no BricsCAD V25/V26 runtime, private-DWG, native UI/geometry, installer, signing or performance PASS is claimed.

The owner goal remains 100%, but a defensible production 100% still requires the applicable `LOCAL_ONLY`, `POLICY_REQUIRED`, `ENGINEERING_REQUIRED`, `FORMAT_SCOPE_REQUIRED` and external-credential gates enumerated in `docs/GOAL-100-REMAINING-2026-08-12.md`. Remote agents should continue evidence-driven source hardening without converting those gates into false remote PASS claims.

## Coordination

Current-main review before registration showed active work around browser bounds/reference canonicality, health null handling, Grid identity, XLSX integrity, audit integrity and other quantity/reporting defects, but no owner for `ProjectQuantityReportBuilder` selection-ID duplicate/lazy-enumeration canonicality or a `GOAL-100-REMAINING` synthesis. During integration, repeated current-main comparisons showed no overlap with the three implementation paths reserved by this claim.

## Completion condition

Completed: the consolidated remaining-work Markdown is present, project-quantity selection IDs fail closed on duplicates without weakening blank/unknown checks, focused regression source is self-registered, direct current-main readback confirms the implementation survived concurrent integration, exact pushed SHA evidence is recorded above, and the remaining production qualification boundary is explicit rather than being falsely marked 100%.
