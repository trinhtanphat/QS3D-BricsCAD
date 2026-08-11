# Agent work claim — SelectionState input bound

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `DONE`
- Scope: bound live semantic selection replacement so arbitrary/lazy input cannot enumerate indefinitely or allocate without limit before `SelectionState` changes.
- Evidence: `SelectionState.Replace(IEnumerable<string>)` previously built a `HashSet` directly from a LINQ pipeline with no enumeration ceiling. The persisted Project Browser workspace already caps selected semantic element IDs at 10,000, so the live selection state now uses the same safety envelope.
- Files reserved during work:
  - `src/QS3D.Core/Services/SelectionState.cs`
  - `tests/QS3D.Core.SmokeTests/SelectionStateInputBoundSmoke.cs`
  - this claim file for close-out
- Implemented:
  1. Added a 10,000 raw-entry ceiling with fast-fail for known generic collection counts and max+1 termination for arbitrary/lazy enumerables.
  2. Candidate selection is fully materialized before `_ids` mutation, so oversize input cannot partially replace selection or emit `Changed`.
  3. Existing valid-input behavior remains: blanks ignored, IDs trimmed, case-insensitive dedupe, deterministic snapshots and canonical-equivalent replacement remains event-silent.
  4. Added CAD-independent smoke coverage for the exact 10,000 boundary, known 10,001 failure, lazy max+1 termination and no partial mutation/event.
- Implementation commit: `c0f48599c2de2cb5d45ada0182149bf88ecafc6d` (`fix(selection): bound selection state input enumeration`).
- Regression commit: `ca90cdac028c8b9a60890eb696910e08874e8bb2` (`test(selection): guard selection state input bound`).
- Integration verification: the regression commit was confirmed reachable from later `main` (`4522f04ba9b845a7bdb64dc936d118a0cdaa3ca2`), and the current source still contains the 10,000-entry preflight/max+1 guards after concurrent unrelated commits.
- Validation boundary: source/smoke contract reviewed remotely. GitHub Actions were not dispatched; the smoke executable was not claimed as executed in this session; the available local container has no .NET SDK, and no licensed BricsCAD V25 runtime PASS is claimed.
- Non-overlap: Project Browser planner/workspace persistence source, WPF/native selection bridge, PICKFIRST/editor code and native V25 runtime behavior were not modified.
- Reservation released: this claim is complete and no longer reserves the listed files.
