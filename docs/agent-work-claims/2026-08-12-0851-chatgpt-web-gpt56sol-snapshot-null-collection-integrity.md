# Work claim — ProjectStateSnapshot null collection-entry integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-snapshot-null-collection-integrity-20260812-0851`
- Registered: `2026-08-12T08:51:00+07:00`
- Completed: `2026-08-12T08:52:00+07:00`
- Baseline main SHA: `11afaeaffa18872a9a92ff376070d640ce6bf2f0`
- Claim commit: `a9e4bd4bdd4ad6dfdd99eaa88cd3d9953a87d0db`
- Source fix commit: `3892290956f59d886896f9a196e62d87fe0da96d`
- Focused smoke commit: `3e18d7ba35364b53c70cfaef1eee3533631da04b`
- Priority: P1 — rollback/preview infrastructure must fail closed on malformed semantic collections.
- Task Key: `CORE-SNAPSHOT-NULL-COLLECTION-ENTRY-INTEGRITY`

## Confirmed defect

`ProjectStateSnapshot.CreateDetachedCopy(...)` reached `CopyInto(...)`, which directly dereferenced Zone, Floor, Family, Element, QuantityRule and AuditEvent entries while cloning them. A malformed project containing a null collection entry could therefore leak an incidental `NullReferenceException` through preview/snapshot infrastructure instead of failing closed under the same semantic integrity expectations used by persistence and diagnostics.

The earlier snapshot null-backing-fidelity lane remains authoritative for nullable field values inside valid objects; this lane addresses null collection objects only.

## Implemented contract

- `CopyInto(...)` now preflights Zones, Floors, Families, Elements, QuantityRules and AuditEvents before writing any target state.
- A null entry fails with `InvalidOperationException("Cannot snapshot a project containing a null <label> entry at index <n>.")`.
- Existing duplicate-element capture checks, element identity preservation, null-backing fidelity and foreign-target isolation are unchanged.
- Persistence schema, authoring policy, UI/native BricsCAD and Level-chain code were not modified.

## Validation evidence

- Current `main` readback confirms collection validation executes at the start of `CopyInto(...)`, before scalar or collection target mutation.
- `ProjectStateSnapshotNullCollectionIntegritySmoke` is auto-registered and covers null Zone, Family and Audit entries plus canonical detached-copy isolation for Zone/Family/Element objects and semantic content.
- This connector-only session did not execute the .NET smoke binary, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: detached snapshot/rollback infrastructure now fails closed on null semantic collection entries instead of leaking incidental null dereferences.
