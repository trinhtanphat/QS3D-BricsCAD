# 2D takeoff revision overlay

`DrawingRevisionOverlay2D` turns canonical `DrawingRevisionComparer2D` results into a deterministic, UX-ready overlay payload for CostX/Autodesk-style drawing revision review.

The overlay preserves both previous/current `TakeoffQuantityEvidence2D` records, including source reference, source handle, classification, zone, layer, unit and quantity, together with the signed quantity delta.

Publication fails closed when delta cardinality is malformed: Added requires current-only evidence, Removed requires previous-only evidence, and Changed/Unchanged require both sides. Evidence markup ids must match the delta id.

Ordering is deterministic by markup id. Existing comparer and evidence APIs remain unchanged, so current callers are source- and binary-compatible. Consumers that need raw deltas may continue using `DrawingRevisionComparer2D`; UI/package workflows should prefer the validated overlay publisher when presenting revision evidence.
