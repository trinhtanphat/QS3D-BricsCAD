# Work claim — Zone target operations global duplicate integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-global-duplicate-integrity-20260812-0855`
- Registered: `2026-08-12T08:55:00+07:00`
- Completed: `2026-08-12T08:57:00+07:00`
- Baseline main SHA: `816e9cc7a0141749c818e315713a1fdbc8d33e15`
- Claim commit: `00f0be9486b9165242de1c5879eb25cdd14c7ef0`
- Source fix commit: `f9f6332bde8bc958cdeda748b586a59b15ae8b5e`
- Focused smoke commit: `4ed81389350e378726fedef023f8fdfa4fce00cb`
- Priority: P1 — target-based Zone operations must reject globally ambiguous Zone identity state.
- Task Key: `CORE-ZONE-TARGET-OPS-GLOBAL-DUPLICATE-ID`

## Confirmed defect

The historical duplicate-Zone fix routed `FindRequired(...)` through `ProjectState.FindZone(targetId)`, which only detects duplicate identities matching the requested target. An unrelated duplicate pair such as `Z1`/`z1` could therefore coexist with unique target `Z2`, allowing target-based Zone operations to continue on globally invalid identity state.

## Implemented contract

- `ValidateUniqueZoneIds(...)` checks case-insensitive uniqueness of all existing non-null Zone IDs.
- `Create(...)` reuses that helper after its existing null-entry guard.
- `FindRequired(...)` invokes the helper before `ProjectState.FindZone(...)`, covering `Update`, `SetActive`, `Assign`, `Delete`, and `ReferenceCount`.
- Existing null behavior remains delegated to Create's null guard / `ProjectState.FindZone`; canonical no-op and valid target semantics remain unchanged.
- Floor/Family services, Floor/Zone UI, persistence/interchange and native BricsCAD code were not modified.

## Validation evidence

- Current `main` readback confirms `FindRequired(...)` globally validates Zone IDs before target resolution.
- `ProjectZoneGlobalDuplicateIntegritySmoke` is auto-registered and exercises Update, SetActive, Assign, Delete and ReferenceCount against `Z1`/`z1` plus unique `Z2`, proving no Zone/element/revision/timestamp mutation on rejection.
- The same smoke preserves valid Update/SetActive/Assign/ReferenceCount behavior on a canonical project.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: target-based Zone operations now fail closed on unrelated duplicate Zone identities before mutation or result production.
