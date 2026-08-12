# Work claim — Locate missing dependency referential integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-locate-missing-dependency`
- Registered: `2026-08-12T08:09:00+07:00`
- Completed: `2026-08-12T08:12:00+07:00`
- Baseline main SHA: `7578ec3638600c76beb54c20223a82ed37a759b2`
- Claim commit: `9a1270e59a687fbe641ce90ad655c284a085578d`
- Priority: P1 — fail-closed semantic Locate on broken dependency relations.

## Confirmed defect

`SourceHandleResolver.Resolve` already rejected blank, non-canonical and duplicate dependency entries before Locate traversal. It built a complete project element index, but a canonical dependency id that was absent from that index was pushed onto the traversal stack and then silently skipped by the generic `TryGetValue(... ) continue` path. Locate could therefore return a partial handle set while hiding a broken semantic relation.

This conflicted with the repository's established dependency referential-integrity behavior in `DependencyGraph.Rebuild`, which fails closed when a semantic dependency targets a missing element.

## Completed scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleResolverMissingDependencySmoke.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleResolverMissingDependencyRegistration.cs`
- this claim file

## Product/test commits

- `ebe17568e6e810d2e2bf8bd32590074ed9085ef6` — `fix(locate): reject missing semantic dependencies`
- `a9faf00389de8e4d5140005ae2f25bb59aeeffac` — `test(locate): cover missing semantic dependency guard`
- `6ba35869805e70567ba2c60108978c3688aae3d5` — `test(locate): register missing dependency smoke`

## Resulting contract

- Root ids that are not project-owned retain the existing empty-result behavior.
- Once traversal reaches a project-owned semantic element, every canonical `DependsOn` id must resolve to a project-owned element before Locate collects/returns that element's handles.
- Broken dependency relations fail closed rather than producing a partial Locate result.
- Valid dependency traversal/order and handle de-duplication remain unchanged.

## Validation

- Re-fetched `SourceHandleResolver.cs` after claim publication and wrote against exact blob SHA `fd577e573cdf03e75d41479d7dcc1085283dcc75`.
- Reviewed implementation commit `ebe17568e6e810d2e2bf8bd32590074ed9085ef6`; the diff only inserts missing-dependency preflight and its helper after existing canonical dependency validation.
- Read back current `main` source and confirmed the guard remains present.
- Focused smoke source covers: canonical missing dependency rejection, valid `E-ROOT -> E-HOST` traversal preserving handle order `AA, BB`, and unchanged unknown-root empty-result behavior.
- Smoke registration uses a dedicated module initializer.
- GitHub Actions were not dispatched.
- No .NET SDK or licensed BricsCAD runtime was available in this session, so no compile/test-runtime/V25/V26 PASS is claimed.

## Excluded scope

- No changes to `DependencyGraph`, diagnostics health services, source-handle ownership policy, generated handle parsing, BricsCAD UI/native selection, or project persistence.
- No auto-repair policy was added for missing relations.

## Completion

Locate now fails closed on canonical missing semantic dependencies on the reserved source-safe Core surface; focused regression source is on `main`, and this claim is released as completed.
