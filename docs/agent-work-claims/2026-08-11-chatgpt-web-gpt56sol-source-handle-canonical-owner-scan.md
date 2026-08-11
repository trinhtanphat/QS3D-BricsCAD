# Agent work claim — Source handle canonical owner scan

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-11 (UTC+7)
- Status: `ACTIVE`
- Scope: make semantic source-handle ownership scans fail closed on malformed in-memory stored SourceHandles instead of inconsistently trimming/ignoring them.
- Evidence: `SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(...)` currently compares stored `ProjectElement.SourceHandles` without canonical validation, while selection ownership mapping trims stored handles before matching. A padded/corrupt stored handle can therefore be missed by capture-owner resolution instead of being rejected as unsafe project state.
- Files reserved:
  - `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipCanonicalSourceSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Validate every stored SourceHandle encountered by semantic ownership scans as nonblank and already trimmed/canonical.
  2. Reuse the same validation in unique capture-owner lookup and selected-handle ownership resolution so both paths have one fail-closed contract.
  3. Preserve user-input normalization/deduplication for selected/query handles; only malformed project-owned stored handles are rejected.
  4. Add CAD-independent smoke coverage for canonical success plus padded/blank stored-handle rejection in both ownership paths.
  5. Refresh `main` around writes, preserve concurrent changes, and release the claim after integration verification.
- Non-overlap: no Source Reconcile adapter command, no Direct Draw, no generated-handle policy, no native CAD mutation, and no currently ACTIVE runtime/feature claim is touched.
- Validation: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25 runtime PASS.
