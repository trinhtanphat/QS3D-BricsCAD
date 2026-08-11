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

The coordinator propagates the exact canonical cleanup set through `NativeCleanupElementIds`. Execution forwards the caller's cleanup authorization to the canonical UseSource implementation unchanged.

Passing non-empty cleanup authorization to AppendOnly, KeepTarget or ImportAsNew fails closed. Cleanup authority must not be silently consumed by a different mutation policy.

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
- required native-cleanup Element IDs.

`Execute(...)` re-plans against the current target immediately before dispatching the canonical importer. A previously displayed plan is informational and is not stale authorization.

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
