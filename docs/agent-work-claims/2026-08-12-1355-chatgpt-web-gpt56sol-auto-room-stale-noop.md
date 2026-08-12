# Agent work claim — Auto Room repeated stale-marking no-op

- Status: ACTIVE
- Owner: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 13:55 +07:00
- Scope: `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`, focused Core smoke coverage, registration if needed, and a static preflight for this contract only.
- Defect: `AutoRoomLifecycle.MarkStaleForSelection(...)` currently treats an Auto Room that is already canonically stale for `TopologyChanged` as a fresh mutation on every identical selection pass. It calls `project.Touch()`, overwrites `BoundaryStaleUtc`, and marks the room dirty even though semantic stale state/reason did not change, producing false-dirty persistence/regeneration work and destroying the original stale timestamp.
- Contract: matched rooms already carrying canonical `BoundaryState=Stale`, a canonical UTC `BoundaryStaleUtc`, and `BoundaryStaleReason=TopologyChanged` are no-ops; newly stale or malformed/incomplete stale metadata is repaired once. Preserve selection freshness, scope/source matching, ordering, UTC validation, and returned changed-room ordering.
- Non-overlap: excludes Auto Room selection freshness, boundary-handle canonicality, Family sync/integrity, generated regeneration, UI/runtime commands, persistence, quantity arithmetic, and all currently active claims outside this exact no-op lifecycle seam.
