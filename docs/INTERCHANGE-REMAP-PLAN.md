# Interchange Import As New — remap dry-run

`QS3DINTERCHANGEREMAPPLAN` is a **plan-only** workflow for future Import As New semantics. It does not mutate the active project or DWG.

The purpose is to make rename/remap deterministic before any importer is allowed to append identities under different IDs/names.

## Deterministic candidates

The planner starts from a strict `QS3D.SemanticSnapshot` v1 validation result.

For incoming semantic IDs that collide with target IDs it allocates candidates in this order:

- `<source-id>-import`
- `<source-id>-import-2`
- `<source-id>-import-3`
- ...

For incoming Zone/Floor/Family display names that collide with target names it allocates:

- `<source-name> (Imported)`
- `<source-name> (Imported 2)`
- `<source-name> (Imported 3)`
- ...

Candidates are bounded to the same schema limits as the snapshot validator: 128 characters for IDs and 512 for names. The allocator reserves target identities/names and all incoming original identities/names so it cannot accidentally create a second collision inside the same import plan.

## Reference rewrite coverage

The planner explicitly resolves typed semantic references:

- `FamilyId`;
- `FloorId`;
- `ZoneId`;
- `DependsOn` Element IDs.

It also recognizes the currently explicit property-carried relation:

- `HostWallId` for Door/WallOpening hosting.

A non-empty `HostWallId` must resolve to an Element contained in the source snapshot. The planner does not guess that an unresolved source host should bind to an unrelated target Element merely because the target happens to have a matching ID.

## Opaque property references are fail-closed

Semantic relations can also exist inside element properties. A property that looks like an identity/reference but has no registered rewrite policy must not be blindly copied during Import As New.

The dry-run therefore reports opaque property-reference warnings. `CanAppendAsNew` is false while any such warning remains.

The current heuristic already catches unregistered ID/ref-like Element relations that point at source Elements. Mutation code must be at least as strict as this planner and may reject additional ID/ref-shaped properties until an explicit relation policy is registered.

This conservative behavior is intentional: a remapped project with one stale embedded ID can look valid while silently pointing to the wrong semantic object.

## Output

The command reports:

- source project ID;
- number of identities;
- number of ID remaps;
- number of display-name remaps;
- number of typed reference rewrites;
- unresolved opaque reference count;
- `READY` or `BLOCKED` for future Append As New execution.

It also prints bounded samples of remaps/reference rewrites/blocks to the BricsCAD command line. Large plans are truncated only for display; the in-memory plan remains complete and deterministic.

## No mutation boundary

`QS3DINTERCHANGEREMAPPLAN` does **not**:

- add/remove/replace Zone, Floor, Family or Element objects;
- change current project active context;
- write native DWG entities;
- adopt source CAD handles;
- clear generated ownership;
- regenerate geometry;
- cut openings;
- save `.qsdb`.

The command exists specifically so relation coverage and candidate names can be reviewed before implementing/executing mutation.

## Future Append As New executor requirements

An executable remap importer must re-plan immediately before mutation and refuse to run unless its plan is still executable. It must:

1. append every incoming identity under the planned target ID/name;
2. rewrite all typed Family/Floor/Zone/dependency references;
3. rewrite only explicitly registered property-carried references such as `HostWallId`;
4. reject unresolved/opaque reference properties instead of guessing;
5. discard incoming `sourceHandles` as target ownership;
6. clear/omit incoming generated/native ownership metadata;
7. give newly appended Elements no fabricated target drawing fingerprint;
8. use `ProjectStateSnapshot` rollback for semantic atomicity;
9. preserve existing target semantic/native state;
10. leave generated geometry, physical cuts and project save explicit.

Exact V25 save/reopen and runtime behavior remains a separate qualification gate.
