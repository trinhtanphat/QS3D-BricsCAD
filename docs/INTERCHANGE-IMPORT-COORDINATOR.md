# Unified Core interchange import coordinator

Status: `SOURCE_IMPLEMENTED` for policy selection/orchestration in `QS3D.Core`. This does not create a BricsCAD command or claim V25 runtime qualification.

`ProjectInterchangeImportCoordinator` is the single Core entry point for selecting **one explicit mode** for semantic snapshot mutation:

- `AppendOnly`
- `KeepTarget`
- `ImportAsNew`
- `UseSourceSemanticData`

The request also states whether `PreserveSourceHandleProvenance` is required.

## No implicit fallback

The coordinator never falls back from one policy to another.

Examples:

- an `AppendOnly` request that collides does not silently become KeepTarget or Import As New;
- a blocked Import As New plan reports blockers and execution refuses rather than trying another policy;
- a UseSource request never degrades to KeepTarget merely because cleanup authorization is missing.

This keeps reviewed user intent and mutation semantics stable between preview and execution.

## Canonical implementations remain authoritative

The Core coordinator does not reimplement import logic. It delegates to the existing canonical paths:

| Requested mode | Provenance off | Provenance on |
| --- | --- | --- |
| AppendOnly | `ProjectInterchangeAppendOnlyImporter` | `ProjectInterchangeAppendProvenanceImporter` |
| KeepTarget | `ProjectInterchangeKeepTargetImporter` | `ProjectInterchangeKeepTargetProvenanceImporter` |
| ImportAsNew | `ProjectInterchangeRemapAppendImporter` | `ProjectInterchangeRemapProvenanceImporter` |
| UseSourceSemanticData | `ProjectInterchangeUseSourceSemanticImporter` | `ProjectInterchangeUseSourceProvenanceImporter` |

The coordinator plan summarizes additions, target identities kept, semantic replacements, ID/name remaps, source-handle count, blockers and native-cleanup requirements without creating another semantic merge engine.

## Cleanup authorization

Only `UseSourceSemanticData` may consume `ProjectInterchangeNativeCleanupAuthorization`.

For UseSource, `ProjectInterchangeImportCoordinatorPlan.NativeCleanupRequirements` preserves the canonical **exact generated-handle requirements**: each requirement contains the target Element ID and the exact generated owner handles observed by the canonical UseSource plan. `NativeCleanupElementIds` remains a convenience/reporting view for UI and is not sufficient cleanup authority by itself.

The retained canonical UseSource plan also records the reviewed target identity and semantic freshness boundary: `TargetProjectId`, `TargetDrawingFingerprint`, and `TargetChangeVersion`. `CreateNativeCleanupAuthorization()` binds cleanup authority to those values as well as the exact generated-handle requirements.

After a guarded native adapter has completed or transactionally staged cleanup for the reviewed requirements, it can call `CreateNativeCleanupAuthorization()` on that same coordinator plan. The returned authorization is created from the retained canonical UseSource plan, so callers do not need to run a second canonical planning pass merely to recover handle-bound authority.

Calling `CreateNativeCleanupAuthorization()` on AppendOnly, KeepTarget or ImportAsNew fails closed. Passing non-empty cleanup authorization to those modes also fails closed. Cleanup authority must not be silently created or consumed by a different mutation policy.

`Execute(...)` still re-plans against current target state in the canonical importer. When native cleanup is required, execution rejects authorization if the target project id changed, the drawing fingerprint changed, the semantic `ChangeVersion` advanced, or the exact generated handle set no longer matches the reviewed plan. This prevents cleanup approval from being replayed across projects, drawings, or stale semantic revisions even when Element IDs and CAD handles happen to collide.

Plans that require no native cleanup continue to execute without manufacturing cleanup authority; target binding is an approval boundary for destructive native-cleanup workflows, not an unrelated requirement for append-only semantic work.

The Core coordinator never performs native cleanup itself.

## Provenance selection

`PreserveSourceHandleProvenance=false` chooses the canonical semantic-only execution path.

`PreserveSourceHandleProvenance=true` chooses the corresponding combined provenance implementation. Imported handles remain provenance only and never become target `ProjectElement.SourceHandles` or target drawing ownership.

The provenance flag is part of the reviewed request; the coordinator does not silently enable or disable it after planning.

## Plan versus execute

`Plan(...)` is non-mutating and returns a normalized `ProjectInterchangeImportCoordinatorPlan` containing:

- requested mode;
- provenance flag;
- source project identity;
- validation-warning count;
- semantic add/keep/replace counts;
- remap counts where applicable;
- source-handle count;
- blocker count;
- exact native-cleanup Element/handle requirements for UseSource;
- convenience native-cleanup Element IDs.

For UseSource, the retained canonical plan additionally captures the target project id, drawing fingerprint and semantic revision used to mint any later cleanup authorization.

`Execute(...)` re-plans against the current target immediately before dispatching the canonical importer. A previously displayed plan is informational until the guarded native cleanup workflow explicitly converts its exact UseSource requirements into authorization.

## Adapter/runtime boundary

This Core coordinator deliberately does not create `QS3DINTERCHANGEIMPORT` or any other BricsCAD UI command.

A production generic import command still needs a reviewed native workflow for:

- previewing the exact mode and provenance choice;
- confirmation/cancel behavior;
- native generated-object cleanup for UseSource;
- native transaction or durable compensation/recovery around cleanup + semantic mutation + rebuild;
- Undo;
- Save/SaveAs/reopen;
- multi-DWG/document switching;
- exact-SHA licensed BricsCAD V25 qualification.

Those adapter/runtime gates remain `LOCAL_ONLY`. Source-level unified orchestration is not equivalent to native round-trip certification.
