# FieldMerge generated-output rebuild

Issue: #3800  
Lane-Key: `issue-3800`

## Purpose

A reviewed `QS3DINTERCHANGEFIELDMERGE` import can invalidate generated dependents. The post-import path may rebuild only outputs explicitly requested for semantic elements invalidated by that same reviewed merge. It must not turn an import into project-wide regeneration.

## Source contract

`InterchangeFieldMergeGeneratedRebuildPlan` is the boundary object for the rebuild phase. The plan:

- accepts only the explicit generated-output kinds `NativeGeometry`, `Quantity`, `Workbook`, and `Trace`;
- uses the repository's canonical string element identity, removes blank ids, trims lookup-equivalent surrounding whitespace, deduplicates case-insensitively, and gives ids deterministic case-insensitive ordering;
- represents empty element/output sets as a no-op;
- fails closed on unknown output flags rather than silently widening scope.

The string-id normalization deliberately matches `ProjectState.FindElement`: semantic element ids are canonical trimmed strings and lookup is case-insensitive. The rebuild boundary must not introduce a parallel `Guid` identity model.

The orchestration layer must construct this plan only after the existing stale-authorization/freshness checks and native invalidation preparation have succeeded. Rebuild execution must remain inside the existing semantic rollback boundary and before final CAD commit. A rebuild failure must therefore abort the import and restore semantic state; it must not leave a partially rebuilt semantic/CAD state.

## Runtime boundary

This lane may make the repository source-complete and add deterministic source tests/preflights. It must not claim licensed BricsCAD runtime evidence, private-DWG evidence, signing evidence, or LOCAL_PASS. Licensed validation remains a local-agent handoff when source acceptance is complete.

## Acceptance still required

Before #3800 is terminal, wire the plan into the FieldMerge orchestration and cover success, no-op, unsupported-output fail-closed behavior, stale authorization, and rebuild-failure rollback. Run repository preflight/Core smoke and V25 compile where CI supports them; otherwise leave licensed-only execution explicitly pending.