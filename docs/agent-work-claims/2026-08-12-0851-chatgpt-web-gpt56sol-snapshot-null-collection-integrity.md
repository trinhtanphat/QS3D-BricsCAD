# Work claim — ProjectStateSnapshot null collection-entry integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-snapshot-null-collection-integrity-20260812-0851`
- Registered: `2026-08-12T08:51:00+07:00`
- Baseline main SHA: `11afaeaffa18872a9a92ff376070d640ce6bf2f0`
- Priority: P1 — rollback/preview infrastructure must fail closed on malformed semantic collections.
- Task Key: `CORE-SNAPSHOT-NULL-COLLECTION-ENTRY-INTEGRITY`

## Confirmed defect

`ProjectStateSnapshot.CreateDetachedCopy(...)` reaches `CopyInto(...)`, which directly dereferences Zone, Floor, Family, QuantityRule and AuditEvent collection entries while cloning them. A malformed project containing a null entry in any of those public mutable lists therefore leaks an incidental `NullReferenceException`. The same snapshot infrastructure is used by preview and transactional rollback flows, while QSDB/domain health contracts already treat null semantic collection entries as invalid state.

The completed snapshot null-backing-fidelity lane preserves nullable field values inside otherwise valid objects; it does not cover null collection objects. The completed element-identity lane covers rollback object identity only.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullCollectionIntegritySmoke.cs`
- this claim file

## Intended contract

- Snapshot copy preflights null entries in Zones, Floors, Families, Elements, QuantityRules and AuditEvents before copying target state.
- Malformed collections fail with stable `InvalidOperationException` messages rather than incidental null dereferences.
- Existing duplicate-element capture checks, element identity preservation, null backing fidelity and foreign-target isolation remain unchanged.
- No persistence schema, authoring policy, UI/native BricsCAD or Level-chain changes.

## Validation plan

Focused auto-registered Core smoke verifies detached-copy rejection for representative null Zone/Family/Audit entries and proves canonical detached copy still preserves project content without sharing mutable semantic objects. Re-fetch moving `main` and exact source before every write. No force-push, Actions dispatch, .NET smoke PASS or BricsCAD runtime qualification claim unless actually executed.
