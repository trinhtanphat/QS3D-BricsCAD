# Interchange field-level precedence

## Purpose

`ProjectInterchangeFieldMergePlanner` is the source-level planning boundary for resolving **same semantic-ID collisions below the whole-identity level**.

The existing import modes remain authoritative and executable as-is:

- AppendOnly
- KeepTarget
- ImportAsNew
- UseSourceSemanticData

Field-level precedence is **not a fifth executable import mode**. It is currently a deterministic preview/planning contract only.

## Explicit precedence

Every supported field group uses one explicit choice:

- `Unspecified` — unresolved; the plan fails closed for mutation design.
- `KeepTarget` — the target value remains authoritative for that changed field.
- `UseSource` — the source value is selected for that changed field, subject to a future executor re-validating runtime/native constraints.

There is no implicit fallback and no timestamp-based winner.

## Supported semantic fields

The planner currently exposes changed values for:

- Zone: `name`
- Floor: `name`, `elevationM`
- Family: `name`, individual `properties.<key>` values
- Element: `familyId`, `floorId`, `zoneId`, `dependencies`, individual `properties.<key>` values, individual `quantities.<key>` values

Map entries are planned per key so additions, removals and replacements are visible independently. Dependencies are compared as case-insensitive semantic-ID sets. Semantic reference IDs use case-insensitive comparison.

Category is **not mergeable**. A same-ID Family or Element whose source and target categories differ is blocked before mutation design.

## Names remain ownership-safe

Choosing a source Zone/Floor name cannot silently take a display name owned by another target semantic ID.

The planner also evaluates selected source names as a batch. Two different same-scope semantic IDs cannot both select the same new source display name merely because neither name was owned by the target before the merge. Zone and Floor names are checked globally within their catalog; Family names are checked within their Family category.

Family display-name ownership remains category-scoped. A source Beam Family name may coexist with the same display name in another Family category, but it cannot silently take a name already owned by another Beam Family.

These planner blockers are not a rename/remap implementation. `ImportAsNew` remains the explicit retain-both/remap policy.

## Portable semantic boundary

The planner derives the target comparison surface through the canonical semantic exporter/validated snapshot path and only enumerates portable semantic fields.

It deliberately does **not** expose any of the following as field-level precedence choices:

- CAD/native generated ownership handles
- source-handle ownership/adoption
- drawing fingerprint ownership
- generated-output bookkeeping metadata
- provenance stores or provenance target maps

A field-level merge policy therefore cannot convert foreign/source CAD handles into active-DWG ownership.

## Generated-output reset signal

A decision may set `RequiresGeneratedOutputReset=true` when choosing the source value could invalidate generated/native output.

This flag is only a **planning requirement signal**. It is not cleanup authorization and does not prove native entities were erased, rebuilt or qualified.

`ProjectInterchangeNativeCleanupAuthorization` remains a separate exact reviewed native-cleanup contract used by executable UseSource flows. Field-level planning must not manufacture or bypass it.

## Meaning of `CanProceedToMutationDesign`

`ProjectInterchangeFieldMergePlan.CanProceedToMutationDesign` means only:

1. planner blockers are empty; and
2. every changed field has an explicit precedence choice.

It does **not** mean the plan is executable, imported, runtime-compatible, natively cleaned, or BricsCAD V25-qualified.

The planner is intentionally marked `IsPreviewOnly` and is intentionally not exposed as a `ProjectInterchangeImportCoordinator` execution mode.

## Requirements before an executor may exist

A future field-level executor must be reviewed separately and, at minimum:

1. re-read/re-plan against the exact current target state immediately before mutation;
2. preserve confirmation freshness and reject stale reviewed state;
3. reuse canonical runtime bounds for Zone/Floor/Family IDs/names and Family property key/value lengths;
4. recheck target capacity and display-name ownership, including same-batch selected source names;
5. validate selected semantic references and dependency ordering after mixed-field application;
6. use canonical Family services so inherited defaults and explicit instance overrides remain correct;
7. compute the complete affected-element/generated-output invalidation closure;
8. require exact handle-bound native-cleanup authorization wherever selected source fields invalidate owned generated output;
9. preserve source/native ownership stripping and provenance separation;
10. apply project mutation transactionally with `ProjectStateSnapshot` rollback and final project validation;
11. remain unavailable through the generic executable coordinator until adapter cleanup/recovery semantics are defined and reviewed.

## Local/native qualification boundary

This planner introduces no new native BricsCAD command and therefore does not create a separate LOCAL_ONLY runtime queue item by itself.

If a field-level executor is later wired into the BricsCAD adapter, its exact-SHA V25 cleanup/rebuild, Undo, save/reopen and multi-DWG qualification must be added to or folded into the existing Interchange scenario in `docs/LOCAL-AGENT-INBOX.md`. Remote agents must not simulate that qualification and must update the existing inbox entry rather than creating a duplicate local queue.

## Current status

Source status: **preview/planning implemented; execution intentionally unavailable**.

This advances the open interoperability work without claiming complete round-trip BIM/native interoperability. External IFC/Revit/BCF/vendor formats, target-DWG handle rebinding/adoption and licensed exact-V25 runtime qualification remain separate boundaries.
