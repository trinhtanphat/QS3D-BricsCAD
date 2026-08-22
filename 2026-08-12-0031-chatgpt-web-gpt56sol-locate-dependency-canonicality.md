# Agent work claim — Locate dependency canonicality

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:31:00+07:00
- Status: `DONE`
- Baseline main SHA: `db883e2575b1d8d7c95cac8e04e831c9e9fc2d1a`
- Scope: make `SourceHandleResolver` fail closed on malformed in-memory `ProjectElement.DependsOn` relations instead of trimming/skipping them during semantic Locate traversal.
- Evidence: Locate traversal previously computed each dependency as `(value ?? string.Empty).Trim()` and skipped blank values. `DependencyGraph` and revision capture now require nonblank, trim-canonical, case-insensitively unique dependency IDs, so Locate had remained a weaker bypass of that relation invariant.
- Files reserved during work:
  - `src/QS3D.Core/Services/SourceHandleResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SourceHandleResolverCanonicalDependencySmoke.cs`
  - this claim file for close-out
- Implemented:
  1. Each visited semantic element now validates its dependency list as nonblank, already trim-canonical and case-insensitively duplicate-free before Locate consumes those relations.
  2. Dependency traversal now pushes the validated canonical IDs directly while preserving the existing reverse-push deterministic order and cycle termination.
  3. Existing direct-source → boundary → generated-owner priority and the 10,000 root input bound remain unchanged.
  4. Added CAD-independent smoke coverage for canonical A→B traversal order plus blank, padded and case-insensitive duplicate dependency rejection with no `ProjectState.ChangeVersion` mutation.
- Implementation commit: `74ae380faf339d6b77654d83fe1f00cef957e0c8` (`fix(locate): reject malformed dependency entries`).
- Regression commit: `966a2cadabfa4b5c4ef18e691b2a8a5de64720b1` (`test(locate): guard canonical dependency entries`).
- Integration verification: at regression publication `main` pointed at `966a2cadabfa4b5c4ef18e691b2a8a5de64720b1`; implementation and regression are sequential and the reserved source was not overwritten between them.
- Non-overlap: no `DependencyGraph`, revision capture, HostLink/Room relation mutation, native `CadHandleService` or WPF/PICKFIRST/editor code was modified.
- Validation boundary: source/smoke contract reviewed remotely. GitHub Actions were not dispatched; the smoke executable was not claimed as run in this session; no licensed BricsCAD V25/V26 runtime PASS is claimed.
- Reservation released: this claim is complete and no longer reserves the listed files.
