# MEP recognition input-bound contract

Issue: #4400  
Lane-Key: `issue-4400`

## Purpose

MEP recognition is a deterministic Core boundary. Public rule/profile constructors accept caller-provided `IEnumerable<T>` values, so they must not consume unbounded input and must agree with the persisted BricsCAD recognition-profile contract.

## Authoritative limits

- at most **100 enumerated token entries per rule**;
- at most **500 enumerated rules per profile**.

The limit applies to traversed token entries before case-insensitive token de-duplication. This is deliberate: an infinite stream of the same token must terminate at element 101 rather than being hidden by normalization. Rule traversal similarly terminates at element 501.

The exact boundaries remain valid: 100 token entries and 500 rules are accepted when all other semantic validation succeeds. Existing null, blank/control-text, duplicate rule-id, discipline/source/MEP-kind and deterministic priority/sort behavior remains unchanged.

`MepRecognitionLimits` in Core owns the numeric contract. The BricsCAD V25/V26 profile persistence layer consumes those Core constants rather than defining an independent 500/100 policy.

## Deterministic qualification

`MepRecognitionSmoke.RecognitionInputBounds()` verifies exact-boundary acceptance, finite oversize rejection, hostile/infinite duplicate token termination, hostile/infinite rule termination, and bounded enumeration counts. `scripts/preflight-mep-recognition-input-bounds.py` guards the source ordering and cross-assembly limit ownership.

This package is repository-safe deterministic integrity work. It does **not** constitute licensed BricsCAD runtime, private-DWG, or `LOCAL_PASS` evidence; historical native MEP workspace/profile runtime qualification remains on its existing LOCAL_ONLY carriers.
