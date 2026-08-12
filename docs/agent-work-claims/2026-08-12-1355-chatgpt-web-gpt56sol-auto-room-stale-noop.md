# Agent work claim — Auto Room repeated stale-marking no-op

- Status: COMPLETED
- Owner: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 13:55 +07:00
- Completed: 2026-08-12 +07:00
- Scope: `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`, focused Core smoke coverage, and a static preflight for this contract only.
- Defect: `AutoRoomLifecycle.MarkStaleForSelection(...)` treated an Auto Room that was already canonically stale for `TopologyChanged` as a fresh mutation on every identical selection pass. It called `project.Touch()`, overwrote `BoundaryStaleUtc`, and marked the room dirty even though semantic stale state/reason did not change, producing false-dirty persistence/regeneration work and destroying the original stale timestamp.
- Resolution: matched rooms already carrying exact canonical `BoundaryState=Stale`, exact `BoundaryStaleReason=TopologyChanged`, and an exact canonical round-trip UTC `BoundaryStaleUtc` are filtered before `project.Touch()`. Newly stale or malformed/incomplete/non-canonical stale metadata remains repairable once. Selection freshness, source/scope matching, UTC validation, and deterministic changed-room ordering are preserved.
- Commits:
  - claim: `09d7163618f9c1a06d4769184465d7db5b2cd3d7`
  - plan: `0283a63e1b4c6a743e8a3ab4b1307ae571886066`
  - source: `4a56eb3ab1a1d4b7874b2a7e9945e0c8d87da220`
  - smoke: `124e9f0c72c0ea04f2622231a666491a8561452e`
  - static preflight: `88c9818c1ef70e9e96dd46db552fc97df10dffc4`
- Verification: source, standalone `[ModuleInitializer]` smoke, and static preflight were committed and read back from current `main`. This connector session did not execute the .NET smoke executable or the Python preflight, did not dispatch GitHub Actions, and did not run BricsCAD V25/V26 licensed runtime qualification.
- Non-overlap: Auto Room selection freshness, boundary-handle canonicality, Family sync/integrity, generated regeneration, UI/runtime commands, persistence, quantity arithmetic, and concurrent external claims were not modified.
