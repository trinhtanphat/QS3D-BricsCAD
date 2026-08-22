# Room boundary diagnostics

Updated: 2026-08-11

## Purpose

`QS3DROOMAUTO` already owns a deterministic planar topology engine in `RoomBoundaryEngine`. The missing source-side gap was diagnostic presentation: a failed auto-room run only reported that no valid closed face was found, so users and local qualification agents could not distinguish empty input, an undersized network, an open network, or faces rejected only by the configured minimum area.

This implementation adds a **read-only diagnostic layer over the existing single topology engine**. It does not introduce a second room-discovery algorithm.

## Core contract

`RoomBoundaryDiagnosticService.Analyze(...)` materializes the selected `BoundarySegment` set once and delegates exactly once to `RoomBoundaryEngine.Discover(..., minimumArea: 0)`. The resulting positive-area candidate faces are then filtered using the requested minimum area with the same strict `Area > minimumArea` rule used by the engine.

The report classifies one of:

- `NoInput` — no boundary segments were supplied;
- `InsufficientSegments` — fewer than three valid segments remain;
- `NoClosedFace` — at least three segments exist, but canonical topology discovery finds no positive-area closed face;
- `BelowMinimumArea` — closed candidate faces exist, but all are rejected by the requested minimum area;
- `Ready` — at least one accepted room boundary exists.

The report also exposes bounded review metadata already produced by the canonical engine: input segment count, distinct source count, candidate/accepted/rejected counts, maximum candidate area, and per-face area/perimeter/source count.

## Provenance and privacy

Boundary source IDs can be BricsCAD handles. Diagnostic presentation therefore **does not expose raw CAD handles or raw boundary geometry keys**. Per-face source provenance and face identity are represented as deterministic SHA-256 fingerprints plus counts. The accepted `RoomBoundary` objects are retained only for the existing in-process authoring workflow; diagnostic presentation fields themselves do not duplicate raw provenance.

## `QS3DROOMAUTO` integration

The command now runs diagnostic analysis immediately after selection/metric settings are resolved and before any canonical project bind or project creation. A no-face result therefore stays non-creating and non-mutating.

Failure messages distinguish:

- no usable boundary input;
- fewer than three valid segments;
- an open/non-cyclic network where no closed face is found;
- valid closed topology whose candidate faces are all at or below `RoomBoundaryMinimumAreaM2`.

Successful runs reuse `diagnostic.AcceptedBoundaries` and continue through the existing project identity freshness check, Room lifecycle, finish synchronization, audit, regeneration and rollback boundaries.

## Validation boundary

Source/static validation is covered by:

- `tests/QS3D.Core.SmokeTests/RoomBoundaryDiagnosticsSmoke.cs`;
- `scripts/preflight-room-boundary-diagnostics.py`.

Exact BricsCAD V25 command-line wording/UX, real LINE/POLYLINE/ARC/SPLINE selection behavior, and representative near-limit performance remain local runtime work. That work is **not a new live queue**: it remains under the existing `LOCAL-010` large-model performance/UI item in `docs/LOCAL-AGENT-INBOX.md`, whose Room performance scope already covers this area. A local agent should add the diagnostic reason matrix to the Room portion of that run and attach sanitized exact-SHA evidence there; remote agents must not manufacture `LOCAL_PASS`.
