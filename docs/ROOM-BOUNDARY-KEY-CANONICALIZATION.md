# Room boundary key canonicalization

Updated: 2026-08-11

## Problem

`RoomBoundaryEngine` uses a canonical quantized boundary key as stable geometry identity. The previous `CanonicalRotation(...)` implementation tried every cyclic start, allocated a full token array for every candidate, joined every candidate with `|`, then selected the ordinally smallest serialized string. For a face with `V` vertices this creates quadratic token-copy/string-comparison work and quadratic transient serialization pressure.

That behavior is deterministic, but it is unnecessarily expensive for finely tessellated Room boundaries such as ARC/SPLINE-derived faces.

## New contract

The engine now selects the lexically minimal cyclic rotation with a linear **minimal-rotation** pass. It compares competing rotations token-by-token and serializes only the final selected rotation once.

The public boundary-key format remains unchanged:

```text
quantized-x,quantized-y|quantized-x,quantized-y|...
```

`BuildBoundaryKey(...)` still canonicalizes both the traced orientation and its reversed orientation, then selects the ordinally smaller result. Room identity therefore remains independent of polygon start vertex and traversal direction.

## Exact lexical compatibility

The old implementation compared complete `string.Join("|", tokens)` candidates with `String.CompareOrdinal`. Comparing raw token strings alone is not always equivalent when one token is a prefix of another, because the serialized separator participates in ordinal ordering.

`CompareRotationToken(...)` therefore compares the conceptual value `token + "|"` without allocating that temporary string. When the token characters match through the shorter token, the comparator uses the literal `|` separator as the next character. This preserves the lexical ordering used by the previous serialized-key implementation while avoiding candidate serialization.

Quantized coordinate tokens are produced by `QuantizedToken(...)` and do not contain `|`, so the separator remains unambiguous.

## Complexity boundary

The minimal-rotation selection performs linear candidate elimination and one final O(V) serialization. `BuildBoundaryKey(...)` runs it once forward and once on the reversed token list, so key canonicalization remains linear in face vertex count apart from the size of the final key string itself.

This change does not alter:

- quantization or tolerance semantics;
- Room face discovery or source provenance;
- broad-phase/intersection/subdivision logic;
- graph/bridge/face tracing;
- minimum-area filtering;
- Room Auto project lifecycle;
- native BricsCAD behavior.

## Deterministic source coverage

`tests/QS3D.Core.SmokeTests/RoomBoundaryKeyCanonicalizationSmoke.cs` exercises a 2,048-vertex closed face and checks deterministic repeated keys. It also constructs the same non-symmetric polygon from shifted and reversed segment orderings and requires identical canonical keys.

`scripts/preflight-room-boundary-key-canonicalization.py` rejects a return to the old every-start rotation loop and locks the separator-aware comparator plus single final serialization contract.

## Runtime evidence boundary

This is source-level complexity hardening, not a measured BricsCAD benchmark. No timing factor or V25 speedup is claimed from static review.

Representative V25/private-DWG Room timings remain `LOCAL_ONLY` under the existing `LOCAL-010 — large-model performance and UI matrix` item in `docs/LOCAL-AGENT-INBOX.md`. The source change does not introduce a new native scenario, so no duplicate local queue item is required.

No source/static evidence from this batch may be promoted to `LOCAL_PASS`.
