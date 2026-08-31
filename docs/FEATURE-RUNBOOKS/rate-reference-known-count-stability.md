# RateReferenceGraph known Count stability

Status: `SOURCE_READY / REMOTE_SAFE`
Lane-Key: `issue-4415`
Runtime: `NOT_APPLICABLE`

## Contract

`RateReferenceGraph` accepts at most 50,000 rate-reference edges and supports both deterministic counted collections and pure streaming `IEnumerable<RateReferenceEdge>` sources.

For a source exposing a supported known Count through generic `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`, the constructor binds mutually consistent Count evidence before traversal. Negative, conflicting, or Count values above the 50,000 ceiling fail closed before enumeration.

During traversal, known Count is an integrity boundary rather than a hint. The first item beyond the admitted known Count is rejected before null-edge, duplicate-edge, key, or other semantic processing. This early overrun rule is independent from the 50,000 streaming ceiling, which remains the authority for pure streaming sources.

After an exactly-sized traversal, deterministic Count evidence is rebound before sorting or publication. A Count that changes, becomes negative, or becomes conflicting across supported Count interfaces fails closed. Under-yield remains rejected after traversal. Honest counted collections retain deterministic sorting and reference-mark semantics, and pure streaming sources remain supported without manufacturing Count evidence.

## Deterministic validation

`RateReferenceGraphKnownCountStabilitySmoke` covers:

- known-count early overrun precedence against an unexpected null edge;
- post-traversal Count drift;
- negative post-traversal Count evidence;
- conflicting post-traversal Count interfaces;
- under-yield rejection;
- stable multi-interface counted input and deterministic sorting;
- pure streaming input.

`scripts/preflight-rate-reference-known-count-stability.py` is auto-discovered by aggregate feature preflight and pins the source ordering, regression registration, 50,000 bound, and this runbook.

## Runtime boundary

This is a host-neutral QS3D.Core cost/data-integrity change. Hosted Core smoke and protected CI are sufficient source evidence. No licensed BricsCAD, private-DWG, signing, or `LOCAL_PASS` claim is required or implied.
