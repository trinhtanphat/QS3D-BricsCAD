# Work claim — Locate missing dependency referential integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-locate-missing-dependency`
- Registered: `2026-08-12T08:09:00+07:00`
- Baseline main SHA: `7578ec3638600c76beb54c20223a82ed37a759b2`
- Priority: P1 — fail-closed semantic Locate on broken dependency relations.

## Confirmed defect

`SourceHandleResolver.Resolve` already rejects blank, non-canonical and duplicate dependency entries before Locate traversal. It builds a complete project element index, but a canonical dependency id that is absent from that index is pushed onto the traversal stack and then silently skipped by the generic `TryGetValue(... ) continue` path. Locate can therefore return a partial handle set while hiding a broken semantic relation.

This conflicts with the repository's established dependency referential-integrity behavior in `DependencyGraph.Rebuild`, which fails closed when a semantic dependency targets a missing element.

## Reserved scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleResolverMissingDependencySmoke.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleResolverMissingDependencyRegistration.cs`
- this claim file

## Intended contract

- Root ids that are not project-owned keep the existing Locate behavior unless already governed elsewhere.
- Once traversal reaches a project-owned semantic element, every canonical `DependsOn` id must resolve to a project-owned element before Locate returns handles.
- Broken dependency relations fail before a partial Locate result is returned.
- Valid dependency traversal/order/handle de-duplication remains unchanged.

## Excluded scope

- No changes to `DependencyGraph`, diagnostics health services, source-handle ownership policy, generated handle parsing, BricsCAD UI/native selection, or project persistence.
- No new auto-repair policy for missing relations.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation plan

- Re-fetch `SourceHandleResolver.cs` after claim publication and write against its exact blob SHA.
- Add focused auto-registered Core smoke source covering a missing canonical dependency failure and an equivalent valid dependency chain that still resolves handles.
- Review exact pushed diffs and read back final source/test from `main`.
- Verify claim/implementation/test/close ancestry on current `main` without force-push.
- No .NET/BricsCAD PASS will be claimed unless actually executed.

## Completion condition

Locate fails closed on a canonical dependency that targets a missing semantic element, valid dependency traversal is preserved, regression source is on `main`, and this claim is closed with exact commit SHAs and truthful validation notes.
