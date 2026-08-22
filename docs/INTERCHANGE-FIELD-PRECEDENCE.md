# Interchange field-level precedence

## Purpose

`ProjectInterchangeFieldMergePlanner` resolves **same semantic-ID collisions below the whole-identity level**. `ProjectInterchangeFieldMergeImporter` is the reviewed Core execution boundary for those plans.

The existing generic import modes remain authoritative and executable as-is:

- AppendOnly
- KeepTarget
- ImportAsNew
- UseSourceSemanticData

Field-level precedence is **not a fifth generic coordinator mode**. The Core planner/executor exists, but it is intentionally not exposed through `ProjectInterchangeImportCoordinator` or a BricsCAD command until native cleanup/recovery orchestration is qualified.

## Explicit precedence

Every supported field group uses one explicit choice:

- `Unspecified` — unresolved; execution fails closed.
- `KeepTarget` — the target value remains authoritative for that changed field/group.
- `UseSource` — the source value is selected, subject to runtime/reference/native-cleanup guards.

There is no implicit fallback and no timestamp-based winner.

## Supported semantic fields

The planner exposes changed values for:

- Zone: `name`
- Floor: `name`, `elevationM`
- Family: `name`, individual `properties.<key>` values
- Element: `familyId`, `floorId`, `zoneId`, `dependencies`, individual `properties.<key>` values, individual `quantities.<key>` values

Map entries are planned per key so additions, removals and replacements remain reviewable. Dependencies are compared as case-insensitive semantic-ID sets. Semantic reference IDs use case-insensitive comparison.

Category is **not mergeable**. A same-ID Family or Element whose source and target categories differ is blocked before execution design.

The Core executor handles same-ID collisions only. If the source contains source-only identities, execution is blocked and the caller must use the explicit AppendOnly or ImportAsNew path instead of receiving an implicit append policy.

## Names remain ownership-safe

Choosing a source Zone/Floor name cannot silently take a display name owned by another target semantic ID.

Selected source names are also evaluated as a batch. Two different same-scope semantic IDs cannot both select the same new source display name merely because neither name was owned by the target before the merge. Zone and Floor names are checked globally within their catalog; Family names are checked within their Family category.

Family display-name ownership remains category-scoped. A source Beam Family name may coexist with the same display name in another Family category, but it cannot silently take a name already owned by another Beam Family.

These blockers are not a rename/remap implementation. `ImportAsNew` remains the explicit retain-both/remap policy.

## Portable semantic boundary

The planner derives the target comparison surface through the canonical semantic exporter/validated snapshot path and only enumerates portable semantic fields.

It deliberately does **not** expose any of the following as field-level precedence choices:

- CAD/native generated ownership handles
- source-handle ownership/adoption
- drawing fingerprint ownership
- generated-output bookkeeping metadata
- provenance stores or provenance target maps

A field-level merge policy therefore cannot convert foreign/source CAD handles into active-DWG ownership.

`ProjectInterchangeJsonExporter` also strips both legacy `PhysicalOpeningCut*` and namespaced `QS3D.PhysicalOpeningCut*` generated/native bookkeeping from semantic property export.

## Reviewed execution authorization

`ProjectInterchangeFieldMergeExecutionPlan` adds the execution-specific guards that the pure planner intentionally does not own.

A reviewed plan binds authorization to:

- exact target `ProjectId`;
- exact target drawing fingerprint;
- exact target `ChangeVersion`;
- SHA-256 of the reviewed source snapshot text;
- deterministic decision stamp for the reviewed per-field choices;
- exact generated owner-handle set for every affected target Element that currently owns generated output.

`ProjectInterchangeFieldMergeImporter.Import` re-reads and re-plans immediately before mutation. Any target revision change, different source snapshot, changed policy/decision surface, or changed generated handle set rejects the old authorization before mutation.

Creating an authorization does **not** erase native entities. When `RequiresNativeCleanup` is true, a future BricsCAD adapter workflow must complete the reviewed native cleanup first and then pass the still-current authorization into Core semantic mutation.

## Runtime and Family semantics

The execution plan fail-closes selected source values that exceed canonical target runtime contracts, including Zone/Floor/Family name limits and Family property key/value limits.

Family changes use canonical services:

- `ProjectFamilyService.Rename`
- `ProjectFamilyService.SetProperty`
- `ProjectFamilyService.RemoveProperty`
- `ProjectFamilyService.Assign`

This preserves inherited Family defaults and explicit instance overrides instead of bypassing Family semantics through raw dictionary replacement.

When an Element changes Family while its Element-property policy keeps target values, the executor preserves the reviewed target property surface after canonical reassignment. When `ElementProperties=UseSource`, it reapplies the portable source property surface while keeping generated/native ownership metadata outside semantic precedence.

## Generated-output invalidation

A `UseSource` decision marked `RequiresGeneratedOutputReset=true` contributes to the affected target closure.

The current Core closure includes:

- the directly changed Element;
- Elements referencing a Floor whose selected source elevation changes;
- members of a Family whose selected source properties change;
- transitive reverse `DependsOn` relations;
- `HostWallId` dependents.

For affected Elements with generated owner handles, the execution plan records exact `ProjectInterchangeNativeCleanupRequirement` entries. After authorization is verified and semantic mutation proceeds, generated ownership metadata is cleared and affected Elements are marked dirty for explicit rebuild.

This does not claim that Core erased or rebuilt native BricsCAD entities.

## Transaction and validation boundary

Execution is rollback-protected with `ProjectStateSnapshot`.

After mixed-field application, the executor rebuilds and validates a canonical portable semantic snapshot. Any invalid combined references/dependency graph/semantic contract causes the project snapshot to be restored.

Audit/provenance metadata is written only inside the guarded Core mutation path. Source CAD handles and source drawing ownership are not adopted.

## Planner versus executor status

`ProjectInterchangeFieldMergePlan.IsPreviewOnly` remains true by design. `CanProceedToMutationDesign` means only that the field decision surface is resolved and planner blockers are empty.

The separate `ProjectInterchangeFieldMergeExecutionPlan.CanExecute` adds source-only and runtime execution blockers, plus target/source/decision/native-cleanup binding required for actual Core mutation.

Neither property means BricsCAD native cleanup, rebuild, Undo, save/reopen, multi-DWG behavior or exact-V25 runtime qualification has passed.

## Remaining adapter/local boundary

The Core executor is intentionally **not** exposed as a `ProjectInterchangeImportCoordinator` execution mode yet.

Before adapter exposure, the BricsCAD layer still needs a guarded reviewed workflow that:

1. shows the exact field decisions and cleanup requirements;
2. re-confirms document/project freshness;
3. erases only the exact authorized generated native objects inside a transaction/recovery boundary;
4. invokes the Core field merge with the reviewed authorization;
5. performs or schedules explicit rebuild without treating stale native output as authoritative;
6. proves rollback/recovery when native cleanup or semantic mutation fails;
7. qualifies Undo, save/reopen, multi-DWG and exact-SHA licensed BricsCAD V25 behavior.

When that adapter path is introduced, its exact-V25 scenario must be folded into the existing Interchange item in `docs/LOCAL-AGENT-INBOX.md`. Remote agents must update that existing item instead of creating a duplicate local queue and must not claim `LOCAL_PASS` from source/static evidence.

## Current status

Source status: **deterministic field planner + reviewed rollback-safe Core executor implemented; generic/native BricsCAD orchestration intentionally unavailable**.

External IFC/Revit/BCF/vendor formats, target-DWG handle rebinding/adoption and licensed exact-V25 runtime qualification remain separate boundaries.
