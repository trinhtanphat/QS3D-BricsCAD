# Interchange field merge mutation design

Status: `SOURCE_IMPLEMENTED / PREVIEW_ONLY` in `QS3D.Core`. This is not a BricsCAD execution path and does not claim V25 runtime qualification.

`ProjectInterchangeFieldMergePlanner` remains the deterministic field-precedence preview. Once every differing field has an explicit `KeepTarget` or `UseSource` decision and no blocker remains, `ProjectInterchangeFieldMergeMutationDesignPlanner` can produce a second, still preview-only envelope describing what a future guarded adapter would have to protect before applying those decisions.

## Target freshness boundary

The mutation design is bound to the exact target:

`ProjectId + DrawingFingerprint + ChangeVersion`

Planning captures those values before field-precedence evaluation and verifies them again after precedence planning and after affected/native-cleanup planning. If project identity, drawing fingerprint, or semantic revision changes during that process, planning fails closed as stale.

A design that requires generated CAD cleanup also requires a non-empty target drawing fingerprint. Destructive cleanup must never be reviewed against an anonymous/unknown drawing boundary.

## Affected target element closure

Only explicit `UseSource` decisions marked `RequiresGeneratedOutputReset` seed the affected set.

The planner maps those decisions to target elements by semantic scope:

- Element decision → that exact target Element;
- Family decision → target Elements using that Family;
- Floor decision → target Elements using the Floor directly or through Bottom/Top Level references;
- Zone decision → target Elements assigned to that Zone.

The planner then expands the affected target element closure through semantic dependencies and `HostWallId` relationships. This prevents a future executor from cleaning only the directly edited object while leaving generated dependents that derive from its old state.

## Exact generated-owner cleanup requirements

For every affected target Element, the design enumerates generated owner slots through `GeneratedHandleOwnershipPolicy`. It records exact generated-owner handle requirements as `ProjectInterchangeNativeCleanupRequirement` entries and validates that every recorded handle resolves to that same semantic owner in project metadata.

The resulting design exposes:

- `AffectedTargetElementIds`;
- `NativeCleanupRequirements`;
- `TargetElementIdsRequiringNativeCleanup`;
- `TargetGeneratedHandlesToClean`;
- exact target project/drawing/revision identity.

Element IDs by themselves are not cleanup authority. A future adapter must operate against the exact reviewed generated-handle set and revalidate target freshness immediately before native work.

## Preview-only boundary

This Core layer deliberately does not execute field mutation and does not erase native CAD.

It does not:

- call an `Execute` path;
- mutate target Family/Floor/Zone/Element fields;
- clear generated ownership metadata;
- erase native objects;
- create a BricsCAD transaction;
- manufacture native-cleanup authorization;
- claim Undo/save-reopen/multi-DWG correctness.

`IsPreviewOnly` therefore remains `true`. The design is evidence for a future adapter review, not proof that native cleanup happened.

## Remaining guarded adapter / V25 boundary

Actual field-level execution remains `LOCAL_ONLY` until a BricsCAD V25 adapter defines and qualifies the full cleanup + semantic mutation + rebuild transaction/recovery contract. That qualification must include cancellation, stale plan rejection, exact generated-owner validation, rollback/failure injection, Undo, Save/SaveAs/reopen, and multi-DWG switching on the exact candidate SHA.

Remote agents may continue improving source-safe planning and Core regression coverage, but must not expose a generic mutation command or claim native runtime PASS from this design envelope alone.
