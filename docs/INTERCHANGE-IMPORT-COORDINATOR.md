# Unified Core interchange import coordinator

Status: `SOURCE_IMPLEMENTED` for policy selection/orchestration in `QS3D.Core`. This does not create a BricsCAD command or claim V25 runtime qualification.

`ProjectInterchangeImportCoordinator` is the single Core entry point for selecting **one explicit mode** for semantic snapshot mutation:

- `AppendOnly`
- `KeepTarget`
- `ImportAsNew`
- `UseSourceSemanticData`
- `FieldMerge`

The request also states whether `PreserveSourceHandleProvenance` is required. `FieldMerge` additionally requires an explicit `ProjectInterchangeFieldMergePolicy`; that policy is rejected for every other mode rather than being silently ignored.

## No implicit fallback

The coordinator never falls back from one policy to another.

Examples:

- an `AppendOnly` request that collides does not silently become KeepTarget or Import As New;
- a blocked Import As New plan reports blockers and execution refuses rather than trying another policy;
- a UseSource request never degrades to KeepTarget merely because cleanup authorization is missing;
- a FieldMerge request with missing precedence policy, unresolved decisions, source-only identities, stale authorization or incompatible runtime/native-cleanup requirements fails closed rather than becoming KeepTarget, UseSource or AppendOnly.

This keeps reviewed user intent and mutation semantics stable between preview and execution.

## Canonical implementations remain authoritative

The Core coordinator does not reimplement import logic. It delegates to the existing canonical paths:

| Requested mode | Provenance off | Provenance on |
| --- | --- | --- |
| AppendOnly | `ProjectInterchangeAppendOnlyImporter` | `ProjectInterchangeAppendProvenanceImporter` |
| KeepTarget | `ProjectInterchangeKeepTargetImporter` | `ProjectInterchangeKeepTargetProvenanceImporter` |
| ImportAsNew | `ProjectInterchangeRemapAppendImporter` | `ProjectInterchangeRemapProvenanceImporter` |
| UseSourceSemanticData | `ProjectInterchangeUseSourceSemanticImporter` | `ProjectInterchangeUseSourceProvenanceImporter` |
| FieldMerge | `ProjectInterchangeFieldMergeImporter` using the reviewed `ProjectInterchangeFieldMergePolicy` | **Unsupported** — the request fails closed instead of silently dropping provenance |

The coordinator plan summarizes additions, target identities kept, semantic replacements, ID/name remaps, source-handle count, blockers and native-cleanup requirements without creating another semantic merge engine. FieldMerge keeps its field-level review metrics separate from identity-level add/keep/replace counters.

## UseSource cleanup authorization

Only `UseSourceSemanticData` may consume `ProjectInterchangeNativeCleanupAuthorization`.

For UseSource, `ProjectInterchangeImportCoordinatorPlan.NativeCleanupRequirements` preserves the canonical **exact generated-handle requirements**: each requirement contains the target Element ID and the exact generated owner handles observed by the canonical UseSource plan. `NativeCleanupElementIds` remains a convenience/reporting view for UI and is not sufficient cleanup authority by itself.

The retained canonical UseSource plan also records the reviewed target identity and semantic freshness boundary: `TargetProjectId`, `TargetDrawingFingerprint`, and `TargetChangeVersion`. `CreateNativeCleanupAuthorization()` binds cleanup authority to those values as well as the exact generated-handle requirements.

After a guarded native adapter has completed or transactionally staged cleanup for the reviewed requirements, it can call `CreateNativeCleanupAuthorization()` on that same coordinator plan. The returned authorization is created from the retained canonical UseSource plan, so callers do not need to run a second canonical planning pass merely to recover handle-bound authority.

Calling `CreateNativeCleanupAuthorization()` on AppendOnly, KeepTarget, ImportAsNew or FieldMerge fails closed. Passing non-empty UseSource cleanup authorization to other modes also fails closed. Cleanup authority must not be silently created or consumed by a different mutation policy.

`Execute(...)` still re-plans against current target state in the canonical importer. When native cleanup is required, execution rejects authorization if the target project id changed, the drawing fingerprint changed, the semantic `ChangeVersion` advanced, or the exact generated handle set no longer matches the reviewed plan. This prevents cleanup approval from being replayed across projects, drawings, or stale semantic revisions even when Element IDs and CAD handles happen to collide.

Plans that require no native cleanup continue to execute without manufacturing cleanup authority; target binding is an approval boundary for destructive native-cleanup workflows, not an unrelated requirement for append-only semantic work.

The Core coordinator never performs native cleanup itself.

## FieldMerge policy and authorization

`FieldMerge` is the reviewed same-ID field-precedence workflow. It does not reuse `ProjectInterchangeNativeCleanupAuthorization` because its authorization must bind the entire reviewed field-merge decision set, source snapshot, target identity/freshness and exact native-cleanup requirement set.

A FieldMerge request must provide `ProjectInterchangeFieldMergePolicy`. The coordinator retains the canonical `ProjectInterchangeFieldMergeExecutionPlan` created by `ProjectInterchangeFieldMergeImporter.Plan(...)` and surfaces dedicated review metrics:

- `FieldMergeSourceFieldsToApply`;
- `FieldMergeTargetFieldsToKeep`;
- `FieldMergeUnresolvedDecisionCount`;
- `FieldMergeSourceOnlyIdentityCount`;
- `FieldMergeAffectedTargetElements`;
- `FieldMergeNativeCleanupHandlesRequired`.

Identity-level `SemanticIdentitiesToAdd`, `TargetIdentitiesToKeep` and `SemanticIdentitiesToReplace` are not repurposed to fake field-level merge counts.

After the caller reviews the coordinator plan, `CreateFieldMergeAuthorization()` mints `ProjectInterchangeFieldMergeAuthorization` from that exact retained canonical execution plan. Execution re-plans again and the canonical FieldMerge importer rejects authorization if the target/source/decision/native-cleanup set no longer matches exactly. Authorization from another source snapshot or stale target therefore cannot be replayed.

FieldMerge currently rejects `PreserveSourceHandleProvenance=true`. Callers that require provenance must choose a provenance-capable identity import mode; the coordinator never silently ignores the requested provenance contract.

Core reports/authorizes required native cleanup but does not claim that BricsCAD entities were erased. Actual native erasure/transaction/rollback/Undo evidence remains an adapter/runtime boundary.

## Provenance selection

For AppendOnly, KeepTarget, ImportAsNew and UseSourceSemanticData, `PreserveSourceHandleProvenance=false` chooses the canonical semantic-only execution path.

`PreserveSourceHandleProvenance=true` chooses the corresponding combined provenance implementation. Imported handles remain provenance only and never become target `ProjectElement.SourceHandles` or target drawing ownership.

The provenance flag is part of the reviewed request; the coordinator does not silently enable or disable it after planning. FieldMerge is explicitly excluded until a product-approved provenance contract exists for field-level merge.

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
- exact native-cleanup Element/handle requirements;
- convenience native-cleanup Element IDs;
- FieldMerge-specific field decision/affected-element/native-cleanup metrics when mode is `FieldMerge`.

For UseSource, the retained canonical plan additionally captures the target project id, drawing fingerprint and semantic revision used to mint any later cleanup authorization.

For FieldMerge, the retained canonical execution plan captures the reviewed source snapshot, target identity/freshness, decision stamp and exact native-cleanup requirements used to mint `ProjectInterchangeFieldMergeAuthorization`.

Execution re-plans against the current target immediately before dispatching the canonical importer. A previously displayed plan is informational until the appropriate reviewed authorization is created when the selected mode requires one.

## Adapter/runtime boundary

This Core coordinator deliberately does not create `QS3DINTERCHANGEIMPORT` or any other BricsCAD UI command.

A production generic import command still needs a reviewed native workflow for:

- previewing the exact mode, provenance choice and FieldMerge precedence policy where applicable;
- confirmation/cancel behavior;
- native generated-object cleanup for UseSource/FieldMerge when required;
- native transaction or durable compensation/recovery around cleanup + semantic mutation + rebuild;
- explicit rebuild of invalidated generated outputs where product policy requires it;
- Undo;
- Save/SaveAs/reopen;
- multi-DWG/document switching;
- exact-SHA licensed BricsCAD V25 qualification.

Those adapter/runtime gates remain `LOCAL_ONLY`. Source-level unified orchestration is not equivalent to native round-trip certification.
