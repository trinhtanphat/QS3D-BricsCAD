# QS3D Direct Draw — Create Similar / Vẽ Tương Tự

Updated: 2026-08-11 (UTC+7)

## Goal

Create Similar reduces the common BLT-style authoring loop from “inspect object → remember Family → find Family → activate → choose draw command” to one explicit sample selection followed by the existing Direct Draw workflow.

```text
QS3DCREATESIMILAR      = select one QS3D sample -> activate its current Family -> Quick Direct Draw
QS3DCREATESIMILARADV   = select one QS3D sample -> activate its current Family -> Advanced/custom Direct Draw
```

The feature does not clone semantic objects and does not introduce another authoring engine. It only resolves the selected object's existing semantic owner and Family/Type, activates that Family through the canonical project mutation contract, then delegates to `QS3DDRAWACTIVE` / `QS3DDRAWACTIVEADV`.

## Accepted sample ownership

One selected CAD object is accepted only when it resolves to exactly one semantic owner through an existing ownership path:

- an exact `ProjectElement.SourceHandles` semantic source; or
- a QS3D-generated output claimed by the shared `GeneratedHandleOwnershipPolicy`.

Generated ownership uses the existing canonical slot rules and ambiguity checks. Create Similar does not scan ad-hoc `Generated*Handle` property names itself.

The command fails closed when the selected CAD object is non-semantic, ownership is ambiguous, the semantic owner has no Family, the Family is missing, or the Family category disagrees with the semantic element category.

## Supported authoring categories

Create Similar asks `ActiveFamilyQuickDrawCommands.SupportsFamily(...)` before changing Active Family. Therefore an unsupported category is rejected without changing Active Family merely to discover later that no safe Direct Draw route exists.

The supported set stays owned by the Active Family dispatcher: ArchitecturalWall, Beam, Column, Slab, GlassWall, WallPier, StructuralWall, Foundation, Door and WallOpening/Window. Create Similar contains no duplicate category-to-command switch.

## Cancel-safe and stale-safe lifecycle

Selection is completed before canonical mutation binding. ESC/cancel at the sample picker returns immediately and does not create/cache-bind a project or change Active Family.

After selection, Create Similar first probes project state through the non-creating `ProjectContextCoordinator.TryGetReadOnly(...)` path. It freezes immutable routing/ownership values:

- `ProjectId` and `ProjectState.ChangeVersion`;
- semantic owner ID;
- Family ID and category;
- source-vs-generated ownership kind;
- canonical generated owner slot when applicable.

Before changing Active Family, the command verifies the same DWG remains active, binds only an already-existing canonical project through `ExistingProjectMutationContext.Require(...)`, requires the same project identity/version, re-resolves the selected handle, and compares the current owner/Family/category/ownership kind with the frozen preview values.

This matters because a read-only probe may expose an in-memory canonical object that can be mutated by modeless UI. Object-reference equality is not treated as a freshness snapshot.

## Mutation boundary

Create Similar owns one intentional semantic mutation:

```text
ProjectFamilyActivationService.SetActive(project, selectedFamily.Id)
```

It does not create geometry, capture semantics, regenerate elements, cut openings, manipulate native ownership, or implement rollback for those operations. After Family activation it synchronously delegates to the existing Active Family dispatcher:

```text
Quick     -> ActiveFamilyQuickDrawCommands.DrawActiveFamily()
Advanced  -> ActiveFamilyQuickDrawCommands.DrawActiveFamilyAdvanced()
```

The delegated workflow retains the current Active-Family immutable routing revalidation, dispatch-scope preview, geometry acquisition, project/unit/UCS/source freshness, semantic capture, scoped regeneration and target rollback behavior.

Changing Active Family is intentional user selection state, matching the existing Workspace “Vẽ Nhanh / Vẽ tùy chỉnh” gesture. If the delegated geometry prompt is later cancelled, no new geometry/semantic element should remain, while the explicitly selected sample Family may remain Active.

## What Create Similar deliberately does not do

- no second Family catalog or semantic model;
- no copy of instance overrides from the sampled element;
- no duplicate Direct Draw category dispatch table;
- no `GetOrCreate` project bootstrap in the Create Similar command;
- no direct calls to Wall/Beam/Column/Slab/opening builders;
- no `SendStringToExecute` queue handoff between Family activation and Active Family dispatch;
- no transient DrawJig/continuous mode claim.

Create Similar means “same Family/Type for the next object”, not “deep clone every instance property”. One-off custom parameters remain the responsibility of `QS3DCREATESIMILARADV` and the existing category-specific Advanced path.

## Runtime qualification boundary

Source/static implementation is REMOTE_DONE only after its focused preflight is merged. Exact BricsCAD V25 interaction is part of existing `LOCAL-008` in `docs/LOCAL-AGENT-INBOX.md`.

Local proof must include at least:

1. ESC at the sample picker is side-effect free and does not bootstrap/cache-bind a project;
2. selecting a live semantic source activates exactly its current Family and Quick dispatch matches `QS3DDRAWACTIVE`;
3. selecting a live generated solid/rebar/mesh output resolves its unique semantic owner and uses that owner's Family;
4. malformed/duplicate generated ownership fails closed;
5. a non-semantic CAD object, missing Family, category mismatch or unsupported Family is rejected before Active Family changes;
6. project replacement/reload, owner remap, Family/category change or source/generated ownership change between preview and canonical bind is rejected;
7. active-DWG switch is rejected without cross-document Family mutation;
8. Quick and Advanced cancellation preserve the existing target-command no-residue contract; intentional Active Family selection may remain;
9. Window-vs-WallOpening routing remains determined by the existing Active Family dispatcher;
10. save/reopen and document switching do not cause the sampled Family from one DWG to drive authoring in another DWG.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source/docs lane does not authorize workflow dispatch.
