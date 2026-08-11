# Agent work claim — Locate dependency canonicality

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:31:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `db883e2575b1d8d7c95cac8e04e831c9e9fc2d1a`
- Scope: make `SourceHandleResolver` fail closed on malformed in-memory `ProjectElement.DependsOn` relations instead of trimming/skipping them during semantic Locate traversal.
- Evidence: current Locate traversal computes each dependency as `(value ?? string.Empty).Trim()` and skips blank values. `DependencyGraph` and revision capture now require nonblank, trim-canonical, case-insensitively unique dependency IDs, so Locate currently remains a weaker bypass of that relation invariant.
- Files reserved:
  - `src/QS3D.Core/Services/SourceHandleResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SourceHandleResolverCanonicalDependencySmoke.cs`
  - this claim file for close-out
- Plan:
  1. Validate each visited element's dependency list as nonblank, already trim-canonical and case-insensitively duplicate-free before pushing dependency roots.
  2. Preserve reverse-push traversal order, cycle termination, direct/boundary/generated-handle priority and the previously added 10,000 root input bound.
  3. Reject malformed relation state before it can redirect Locate to a normalized dependency target.
  4. Add CAD-independent smoke coverage for canonical dependency traversal plus blank/padded/duplicate rejection and read-only failure.
  5. Refresh current `main`, verify reachability/current source, then release the claim.
- Non-overlap: no `DependencyGraph`, no revision capture, no HostLink/Room relation mutation, no native `CadHandleService` and no WPF/PICKFIRST/editor work.
- Validation boundary: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25/V26 runtime PASS.
