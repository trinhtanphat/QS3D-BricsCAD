# V25 Column Tie document-generation affinity

## Scope

This carrier hardens `QS3DREBARTIES3D` in `src/QS3D.BricsCAD.V25/ColumnTieCommands.cs`. It does not change Column Tie geometry arithmetic, project persistence semantics, Quantity engine behavior, MCP runtime/transport, installer, or release flows.

## Failure mode

The command captures the active managed BricsCAD `Document`, snapshots PICKFIRST `ObjectId` values, binds the existing mutable QS3D project, and then hands those retained native IDs to `ColumnTieSolidBuilder.BuildSelected(...)`.

Project/backing-store freshness does not prove that the same native `Database` generation is still authoritative. During host/UI pumping, the active MDI document can change, or a managed document wrapper can survive while its native database generation is replaced. Without an exact managed-document plus native-database fence immediately before geometry mutation, stale `ObjectId` values can be handed to a non-authoritative generation.

After a successful native/project mutation, palette refresh, editor regeneration, status publication, and editor output are additional host/UI boundaries. A document-generation change there must not publish into stale context or make a committed mutation appear to have failed.

## Production contract

At command entry, capture the exact managed `Document` and its non-zero `document.Database.UnmanagedObject` identity. Treat both together as the generation authority.

Before `ColumnTieSolidBuilder.BuildSelected(...)`, require that:

1. the captured managed `Document` is still `Application.DocumentManager.MdiActiveDocument`; and
2. its current `Database.UnmanagedObject` equals the captured native identity.

Generation loss fails closed before the retained `ObjectId` geometry handoff.

After builder return, UI synchronization is best-effort and generation-safe:

1. fence before palette/model-tree refresh;
2. revalidate after refresh and before `Editor.Regen()`;
3. revalidate after Regen and before palette status;
4. revalidate after status and before editor output;
5. suppress stale-generation diagnostics;
6. redact post-commit exception text to the exception type only.

A post-commit UI failure must not roll back or falsely report the already-completed CAD/project mutation as failed.

## Transaction and rollback ownership

`ColumnTieSolidBuilder` retains authority over native write transactions and semantic rollback. This carrier adds only pre-handoff generation admission and post-commit UI fences. It does not introduce a second transaction, duplicate project snapshot, or competing rollback path.

The PICKFIRST snapshot remains captured once and is passed unchanged to the builder after generation admission.

## REMOTE_SAFE validation

Hosted/source evidence may include:

1. `python scripts/preflight-v25-column-tie-document-affinity.py`;
2. generic and auto-discovered feature source guards;
3. deterministic smoke tests and V25 package-integrity checks;
4. trusted/admitted locked-reference BricsCAD V25 compilation;
5. static comparison with the established Beam Rebar/Stirrup generation-affinity pattern.

Remote compile/preflight evidence validates source/API compatibility and deterministic contracts only. It is not licensed BricsCAD runtime evidence.

## LOCAL_ONLY qualification

These scenarios remain `LOCAL_ONLY / NO_RESULT` until executed in a real licensed BricsCAD V25 host:

- MDI A→B and A→B→A around PICKFIRST/project binding/native mutation;
- same managed `Document` wrapper with native database reload/replacement;
- stale or disposed `ObjectId`/database wrappers;
- document-generation change during palette refresh or `Editor.Regen()`;
- visible model/palette/status behavior after successful authoring;
- native transaction/rollback behavior under host exceptions.

Never infer `LOCAL_PASS` from Shared/Hybrid CI or admitted-reference compilation.

## Self-review checklist

- Exact managed-document/native-database generation captured at entry.
- Retained `ObjectId` geometry handoff fenced immediately before builder invocation.
- Existing project/backing-store authority remains unchanged.
- Builder transaction and rollback ownership preserved.
- No new subscriptions, modeless roots, disposal obligations, or cancellation source.
- Post-commit refresh/Regen/status/output are ordered and generation-safe.
- UI sync failure cannot become a false CAD/project rollback claim.
- Stale diagnostics suppressed; exception message content redacted.
- Thread/host affinity remains on the existing command path.
- V25 API compatibility verified by admitted locked references.
