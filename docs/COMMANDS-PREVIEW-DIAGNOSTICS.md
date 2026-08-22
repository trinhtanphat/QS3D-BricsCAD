# QS3D Preview / Diagnostic commands

Updated: 2026-08-10.

These commands are source-implemented in the BricsCAD V25 adapter. They still require exact-SHA licensed V25 runtime qualification before customer-release claims.

## `QS3DRULEPREVIEW`

Read-only quantity-rule dry run for the active project.

Behavior:

- resolves the active DWG-bound `ProjectState`;
- clones semantic state through `ProjectStateSnapshot.CreateDetachedCopy`;
- runs the real `QuantityRuleEngine` against the detached copy;
- reports element/output Add / Changed / Removed deltas;
- reports rule provenance changes;
- does not call Apply, regeneration on the live project, `project.Touch()` or native CAD mutation;
- preview objects carry the source `ProjectState.ChangeVersion` so a later guarded Apply can reject a preview after any tracked project change.

Current adapter command is intentionally preview-only. Core has guarded Apply APIs, but production mutation UX still needs explicit confirmation/Cancel/Undo/session qualification in BricsCAD V25.

## `QS3DREGENPREVIEW`

Read-only semantic regeneration dry run.

Behavior:

- clones the active project;
- runs the real default `RegenerationEngine.RegenerateDirty` on the detached copy;
- compares before/after semantic state through `RevisionService`;
- reports regenerated count, changed elements and changed fields;
- computes Model Health New / Resolved / Persistent differences;
- reports new Health Error count;
- leaves live semantic/native project state untouched;
- binds the preview to the source `ProjectState.ChangeVersion` for stale-preview rejection by the Core guarded Apply API.

This does not materialize or test native Solid3d output. It previews the Core semantic regeneration layer only.

## `QS3DDIAGSUMMARY`

Exports a privacy-safe aggregate diagnostic JSON.

Current `QS3D.DiagnosticSummary` v1 deliberately includes only:

- project schema version;
- aggregate Zone/Floor/Family/Element/QuantityRule counts;
- dirty/null-entry counts;
- element-category counts;
- Model Health severity counts;
- Model Health code + severity + count aggregates.

It deliberately excludes:

- project ID/name;
- DWG path/fingerprint;
- source/generated CAD handles;
- Zone/Floor/Family/Element IDs or names;
- semantic properties or quantities;
- health messages and per-element IDs.

The file is published through `AtomicFileCommit` rather than direct overwrite.

## Core guarded mutation APIs

Source now also contains guarded mutation APIs for a later V25 UX:

- `QuantityRulePreviewService.ApplyElement(...)`;
- `QuantityRulePreviewService.ApplyProject(...)`;
- `QuantityRulePreviewService.ApplyProjectWithHealthGuard(...)`;
- `RegenerationPreviewService.Apply(...)`.

Their current source contract includes:

- project identity checks;
- `ProjectState.ChangeVersion` stale-preview rejection;
- outcome-equivalence recheck;
- project snapshot rollback;
- Model Health regression blocking/rollback for guarded batch apply.

Do not expose these as an automatic production mutation button without the local confirmation/Undo/session qualification described in `docs/LOCAL-PREVIEW-DIAGNOSTIC-QUALIFICATION-2026-08-10.md`.

## Static source guards

Relevant guards:

```text
python scripts/preflight-quantity-rule-preview.py
python scripts/preflight-regeneration-preview.py
python scripts/preflight-model-health-baseline.py
python scripts/preflight-project-diagnostic-summary.py
python scripts/preflight-rule-preview-diagnostic-commands.py
```

Their presence is source evidence only. A repository edit session must not claim they passed unless they were actually executed on the exact SHA.
