# Room boundary sweep broad-phase

Updated: 2026-08-11

## Why this exists

`RoomBoundaryEngine` already bounded input at 5,000 source segments and subdivision at 20,000 raw edges, but its intersection preparation still visited every `i,j` source pair before checking whether their tolerance-expanded bounding boxes overlapped. Sparse plans therefore paid an avoidable quadratic broad-phase cost even though almost every pair could be rejected spatially.

This batch replaces that direct all-pairs scan with a deterministic **sweep broad-phase** inside the existing `RoomBoundaryEngine`. It is a performance/correctness hardening of the canonical engine, not a second room topology implementation.

## Algorithm contract

For each validated `BoundarySegment`, Core builds the same tolerance-expanded X/Y bounds that were previously used only after entering the nested pair loop. Bounds are sorted deterministically by `MinX`, `MaxX`, `MinY`, `MaxY`, then original segment index.

The sweep keeps an active set containing only segments whose expanded `MaxX` can still overlap the current segment's `MinX`. Expired entries are removed before candidate checks. Only active entries whose full expanded X/Y bounds overlap are forwarded to the existing `CollectPairCuts(...)` intersection/subdivision logic.

The broad-phase therefore removes pairs that the previous `SegmentBounds.Overlaps(...)` check would have rejected anyway. It **does not change**:

- line/segment intersection mathematics;
- endpoint tolerance handling;
- colinear endpoint cuts;
- cut deduplication;
- raw-edge subdivision behavior;
- graph construction, bridge removal or face tracing;
- boundary keys/source provenance;
- `MaxInputSegments = 5000`;
- `MaxSubdividedEdges = 20000`;
- Room Auto project lifecycle or native BricsCAD behavior.

Worst-case dense inputs can still require quadratic candidate work because every expanded rectangle may genuinely overlap. This patch intentionally does not weaken correctness or invent a smaller arbitrary pair cap to make synthetic benchmarks look faster.

## Deterministic source coverage

`tests/QS3D.Core.SmokeTests/RoomBoundaryBroadPhaseSmoke.cs` adds source-safe regression cases for:

- a 4,500-segment sparse network plus one valid room near the existing input ceiling;
- a T-junction that must still subdivide into two adjacent rooms;
- near-endpoint closure that depends on tolerance-expanded bounds.

`scripts/preflight-room-boundary-broad-phase.py` rejects a return to the old direct `for i / for j` all-pairs scan and locks the sweep/pruning and existing safety-limit contracts.

## Performance evidence boundary

This is structural complexity hardening, not a runtime benchmark. **No Stopwatch** timing or made-up speedup factor is used as source evidence.

Representative BricsCAD V25/private-DWG timings remain `LOCAL_ONLY` under the existing `LOCAL-010 — large-model performance and UI matrix` item in `docs/LOCAL-AGENT-INBOX.md`. This source change does not introduce a new local scenario; the existing Room performance scope already covers measuring the resulting candidate SHA on representative projects.

No source/static result from this batch may be reported as `LOCAL_PASS` or as a measured V25 speedup without exact-SHA local evidence.
