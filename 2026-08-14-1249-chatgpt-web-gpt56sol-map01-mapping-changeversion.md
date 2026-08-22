# Agent work claim — MAP-01 mapping mutation ChangeVersion integrity

Status: `COMPLETED`

Agent: `chatgpt-web-gpt56sol-map01-mapping-changeversion-20260814-1249`

Registered: `2026-08-14T12:49:24+07:00`

Completed: `2026-08-14T12:55:41+07:00`

Baseline `main`: `fc7e4d2ecf6abd165d65146ee61e991ad3e579ec`

Priority: `P1` Core semantic-integrity hardening / MAP-01 mapping domain contract.

## Confirmed source gap

`ProjectState.MeasurementWorkItemMappings` is project-owned canonical semantic state persisted in QSDB v4 and consumed directly by MAP-02 coverage. Its collection mutated the reserved `QS3D.Mapping.v1.*` metadata entries on `Add`, successful `Remove`, and non-empty `Clear` without incrementing `ProjectState.ChangeVersion` or updating the project persistence timestamp.

That permitted two different canonical semantic mapping states to carry the same project semantic version. It also diverged from established persisted semantic-catalog mutation policy in this repository, where the project revision is advanced before the persisted write so `ChangeVersion` overflow fails before semantic state changes.

The existing mapping codec/catalog validation remains authoritative; this lane only changes project mapping-collection mutation/version semantics.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMeasurementWorkItemMappingCollection.cs`
- `src/QS3D.Core/Domain/ProjectState.cs` only to bind the mapping collection to its owning project revision.
- `tests/QS3D.Core.SmokeTests/Map01bMappingPersistenceSmoke.cs`
- this claim file.

## Implemented

- `ProjectState` now constructs the measurement/work-item mapping collection with the owning project.
- Mapping `Add` fully validates/canonicalizes the prospective catalog and encoded value before `ProjectState.Touch()`, then commits the reserved metadata write.
- Successful `Remove` resolves the existing canonical mapping before `Touch()` and persisted removal; a missing mapping remains a revision-neutral no-op.
- `Clear` captures the reserved mapping keys and validates the current catalog before `Touch()`; non-empty clear advances the project revision once, while empty clear remains revision-neutral.
- The temporary compatibility constructor used to keep direct-to-main staged publication compile-safe was removed after `ProjectState` binding, leaving no internal collection-construction bypass in the final source.

## Acceptance result

1. Add advances project `ChangeVersion` exactly once before the persisted mapping write: covered by focused smoke source.
2. Existing Remove advances exactly once; missing Remove is a no-op: covered.
3. Non-empty Clear advances exactly once; empty Clear is a no-op: covered.
4. Rejected duplicate Add remains revision-neutral, and forced `ChangeVersion == long.MaxValue` Add is asserted to fail before timestamp/version/mapping metadata changes: covered.
5. Existing mapping codec, identity/ambiguity rules, QSDB v4 format, MAP-02/MAP-03 logic, unrelated metadata, and native boundaries were not changed.

## Commits on `main`

- Claim: `efd7b80d106831e45553e685cb0448fdca669542`
- Compile-safe owner-aware collection preparation: `a12d1dd62c95077bea190dfe27df8dab7407523a`
- Bind collection to `ProjectState`: `e8061cf68d1721bdbeead0a2bef815f7014b3985`
- Remove legacy ownerless constructor / finalize source invariant: `b0fa998d78b8399bfbbf9561f405e90e3fc40052`
- Focused regression: `0a35301c4da4373acc62fbe57de0d4cfec7e7e29`

Two attempted whole-ref atomic publications were rejected by GitHub as non-fast-forward while concurrent agents advanced `main`; neither was force-pushed. The final staged commits above were published through blob-SHA-checked Contents API writes and reconciled with concurrent changes.

## Verification

- Remote `main` was re-read at `0a35301c4da4373acc62fbe57de0d4cfec7e7e29` after the regression commit.
- Final remote collection source was verified owner-bound with pre-write `Touch()` semantics and no legacy ownerless constructor.
- Final remote `ProjectState` source was verified to construct `ProjectMeasurementWorkItemMappingCollection(this, Metadata)`.
- Final remote smoke source was verified to register exact-once/no-op/overflow-before-write assertions.
- `b0fa998d78b8399bfbbf9561f405e90e3fc40052` was verified as an ancestor of the regression head; concurrent claim `docs/agent-work-claims/2026-08-14-1253-gpt56sol-quantity-mutation-persistability.md` remained separate and untouched.
- GitHub Actions: `NOT_RUN` / not dispatched.
- .NET Core smoke execution: `NOT_RUN` because this environment has no `dotnet` executable.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claimed.
- Force push: not used.

## Explicit non-scope retained

- No mapping schema/codec format change or QSDB schema-version bump.
- No MAP-02/MAP-03 coverage business-logic or report/UI change.
- No recognition/template layer-mapping work.
- No BricsCAD/native host changes or qualification claims.
- No broad `ProjectMetadataDictionary` semantic-versioning change; presentation/non-semantic metadata remains outside this lane.
