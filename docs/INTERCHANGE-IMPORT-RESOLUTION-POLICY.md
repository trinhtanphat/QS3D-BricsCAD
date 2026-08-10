# QS3D Semantic Snapshot — explicit import-resolution policy plan

Updated: 2026-08-10 (UTC+7)

`ProjectInterchangeImportResolutionPlanner` is a **non-mutating policy-resolution layer** between the existing immutable/validated Semantic Snapshot model and any future importer.

It exists so a future import command cannot silently invent collision, project-identity, drawing-provenance or generated-output behavior.

## No implicit defaults

`ProjectInterchangeImportPolicy` requires explicit choices for:

- Zone collision: `KeepTarget` or `UseSourceSemanticData`;
- Floor collision: `KeepTarget` or `UseSourceSemanticData`;
- Family collision: `KeepTarget` or `UseSourceSemanticData`;
- element collision: `KeepTarget` or `UseSourceSemanticData`;
- project ID: `RequireMatch` or `AllowDifferent`;
- drawing fingerprint: `RequireMatch` or `AllowDifferentOrUnknown`;
- source Handle provenance: `Discard` or `PreserveAsProvenanceOnly`.

`Unspecified` is deliberately non-executable. Unsupported enum values also fail closed.

Generated-output reset may remain unspecified when no existing element is being replaced from source semantic data. If an existing element is resolved to `UseSourceSemanticData`, the only currently safe planned reset is:

```text
ClearOwnershipAndRequireRebuild
```

The planner refuses to treat existing generated/native CAD as trustworthy after source semantic replacement.

## Identity plan

Every source Zone/Floor/Family/element becomes one read-only resolution item:

- `AddSourceSemanticData` — source ID is new in the target;
- `KeepTarget` — explicit policy keeps the target identity;
- `UseSourceSemanticData` — explicit policy selects source semantic data;
- `BlockedIncompatible` — same Family/element ID has a different `ElementCategory`;
- `Unresolved` — a required collision policy was not explicitly selected.

Category-incompatible Family/element IDs cannot be forced through `UseSourceSemanticData`; rename/remap is a separate policy that has not been specified.

The plan is bounded to 50000 identity items and target duplicate IDs fail closed.

## Project and drawing provenance

`RequireMatch` for project ID or drawing fingerprint creates a global block when the source/target relationship does not satisfy that policy.

`AllowDifferent` / `AllowDifferentOrUnknown` means only that the policy-review layer accepts the identity difference for further design. It does not authorize mutation by itself.

## Drawing-local source Handles

`PreserveAsProvenanceOnly` means exactly that: retain the portable string as source provenance if a future importer is designed to do so. It never means the same numeric Handle in the target DWG is owned or should be rebound automatically.

`Discard` means a future importer would not carry those drawing-local source references into imported semantic state.

Neither choice performs any rebinding in this planner.

## Generated/native output

When an existing semantic element is selected for source replacement, the resolution item records `RequiresGeneratedOutputReset=true` and the overall policy must explicitly choose `ClearOwnershipAndRequireRebuild`.

This is a planning invariant only. `ProjectInterchangeImportResolutionPlanner` does **not** clear properties, erase native entities, mark stale state, regenerate CAD or write `.qsdb`.

A future mutating importer must implement generated-output reset through the canonical ownership/stale/transaction APIs and prove the lifecycle separately.

## Result meaning

`CanProceedToMutationDesign=true` means only:

- all required policy choices are explicit;
- project/fingerprint requirements are satisfied;
- there are no category-incompatible identity blocks.

It does **not** mean import is approved, safe to execute, or implemented. Property/quantity precedence, dependency mutation ordering, catalog replacement semantics, rollback/audit, source-handle application, actual generated-output reset, UI confirmation and exact V25 behavior still require separate implementation/review.

## Source checks

```text
python scripts/preflight-interchange-import-resolution.py
```

`ProjectInterchangeImportResolutionPlannerSmoke` covers no-default policy behavior, all-new read-only planning, keep/replace collisions, mandatory generated-output reset, category mismatch, project/fingerprint requirements, source Handle provenance choice and unsupported enum rejection.

## Current boundary

The source-safe interchange pipeline can now review:

```text
export -> validate -> immutable typed read -> semantic diff -> target collision preview -> explicit non-mutating resolution plan
```

There is still **no `QS3DINTERCHANGEIMPORT` and no JSON round-trip claim**. A mutating importer remains intentionally blocked until the remaining mutation, precedence, rollback, audit, UX and V25 adapter contracts are implemented and qualified.
