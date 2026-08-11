# Agent work claim — Locate source-handle canonicality

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: make Core semantic Locate/source-handle traversal fail closed on malformed direct `ProjectElement.SourceHandles` entries instead of trimming/skipping them and potentially falling back to a different boundary/generated artifact.
- Evidence: `SourceHandleResolver.AddDirectHandles(...)` currently trims each stored direct SourceHandle and silently skips blank values. That makes corrupted ownership state look like “no direct source”, after which the resolver may deliberately fall back to boundary or generated-owner handles. Direct source ownership is authoritative, so malformed stored direct ownership must not be converted into a fallback decision.
- Files reserved:
  - `src/QS3D.Core/Services/SourceHandleResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SourceHandleResolverCanonicalDirectSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Require every encountered direct SourceHandle to be nonblank and already trim-canonical before it can establish direct-reference authority.
  2. Preserve requested element-ID normalization, dependency traversal order, boundary fallback and canonical generated-owner fallback for valid elements that truly have no direct SourceHandles.
  3. Reject malformed direct ownership before boundary/generated fallback can occur.
  4. Add CAD-independent smoke coverage proving canonical direct priority plus padded/blank direct-handle rejection even when generated fallback metadata exists.
  5. Refresh `main` around writes, preserve concurrent work, verify commits remain reachable and release the reservation.
- Non-overlap: no `CadHandleService`, no BricsCAD editor/PICKFIRST code, no generated-handle policy mutation, no Room boundary serialization change, and no native V25 runtime work.
- Validation: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25 runtime PASS.
