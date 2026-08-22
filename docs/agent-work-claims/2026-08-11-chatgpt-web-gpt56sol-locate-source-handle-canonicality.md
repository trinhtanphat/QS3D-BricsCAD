# Agent work claim — Locate source-handle canonicality

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `DONE`
- Scope: make Core semantic Locate/source-handle traversal fail closed on malformed direct `ProjectElement.SourceHandles` entries instead of trimming/skipping them and potentially falling back to a different boundary/generated artifact.
- Evidence: `SourceHandleResolver.AddDirectHandles(...)` previously trimmed each stored direct SourceHandle and silently skipped blank values. That made corrupted ownership state look like “no direct source”, after which the resolver could deliberately fall back to boundary or generated-owner handles. Direct source ownership is authoritative, so malformed stored direct ownership must not be converted into a fallback decision.
- Files reserved during work:
  - `src/QS3D.Core/Services/SourceHandleResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SourceHandleResolverCanonicalDirectSmoke.cs`
  - this claim file for close-out
- Implemented:
  1. Direct SourceHandles encountered by Locate are now required to be nonblank and already trim-canonical.
  2. Valid direct handles remain authoritative and preserve existing deterministic handle order/deduplication.
  3. Malformed direct ownership now throws before Room boundary or generated-owner fallback can run.
  4. Requested element-ID trim normalization, dependency traversal, valid boundary fallback and canonical generated-owner fallback remain unchanged.
  5. Added CAD-independent smoke coverage for canonical direct priority and padded/blank direct-handle rejection while generated fallback metadata is present, including no `ChangeVersion` mutation.
- Implementation commit: `da48efd19021ad05aa9dbfb727769e957e6b7e1d` (`fix(locate): reject malformed direct source handles`).
- Regression commit: `a2201ab96f08e0f8b896602d051c8cad70719688` (`test(locate): guard canonical direct source handles`).
- Integration verification: the implementation commit was confirmed reachable from a later `main`, and at close-out `main` pointed at the regression commit with the current source still containing the fail-closed direct-handle guard.
- Validation boundary: source/smoke contract reviewed remotely. GitHub Actions were not dispatched; the smoke executable was not claimed as executed in this session; no licensed BricsCAD V25 runtime PASS is claimed.
- Non-overlap: `CadHandleService`, BricsCAD editor/PICKFIRST code, generated-handle ownership policy, Room boundary serialization and native V25 runtime behavior were not modified.
- Reservation released: this claim is complete and no longer reserves the listed files.
