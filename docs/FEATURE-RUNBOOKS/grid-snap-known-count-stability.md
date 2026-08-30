# Grid LINE/ARC snap bounded-input Count stability

## Scope

`GridLineSnapPlanner.TryFindNearest` and `GridArcSnapPlanner.TryFindNearest` accept caller-controlled `IEnumerable<GridReferenceCurve>` input with a hard ceiling of 2,000 curves. This contract keeps both snap policies on one shared bounded-input admission path.

## Required ordering

For sources exposing `ICollection<GridReferenceCurve>.Count`, `IReadOnlyCollection<GridReferenceCurve>.Count`, or non-generic `System.Collections.ICollection.Count`:

1. bind all available Count surfaces before traversal;
2. reject negative, conflicting, or greater-than-2,000 metadata before `MoveNext`/`Current`;
3. rebind the exact admitted Count contract before `MoveNext`;
4. after a successful `MoveNext`, rebind and validate Count again before known-count/cap admission and before `Current`;
5. reject known-count overrun before the affected `Current` read;
6. validate the terminal edge and final Count, including under-yield and drift;
7. preserve ordinary pure-streaming sources that expose no Count surface.

The materializer is CAD-independent and does not change LINE/ARC duplicate-ID, type, finite geometry, nearest-point, ambiguity, distance, or deterministic tie-breaking semantics.

## Regression evidence

`GridSnapKnownCountStabilitySmoke` instruments Count, `MoveNext`, and `Current` independently. It proves both LINE and ARC reject over-cap metadata before traversal, reject transient Count growth after the first successful `MoveNext` before `Current`, cover negative/conflicting Count evidence, and retain stable counted plus pure-streaming controls.

`preflight-grid-snap-known-count-stability.py` is auto-discovered by Shared CI and prevents either snap planner from returning to `Take(MaxCurves + 1).ToList()` caller materialization. The guard verifies both parts of the abstraction contract: `ValidateKnownCount(...)` must itself re-read supported Count surfaces via `ReadKnownCount(...)`, and the materializer must invoke `ValidateKnownCount(curves, admittedCount, label)` after `MoveNext()` and before any `Current` read. This intentionally validates behavior across the helper boundary rather than requiring an implementation-detail `ReadKnownCount` call at the traversal site.

## Runtime disposition

Core-only deterministic contract. Licensed BricsCAD/private-DWG runtime is not required and must not be claimed from this evidence.
