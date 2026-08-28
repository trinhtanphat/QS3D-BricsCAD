# Issue #4316 — IFC round-trip known-count processing boundary

Lane-Key: `issue-4316`

## Problem

The IFC round-trip projection/result stack accepts both ordinary streaming `IEnumerable<T>` inputs and collections that expose a trustworthy `Count`. Before #4316, the affected collection boundaries validated negative/conflicting/oversized Count metadata before traversal and rejected final Count/traversal disagreement afterward, but an over-yielding counted source could allow item `knownCount + 1` to enter semantic processing before the mismatch was reported.

That ordering could let an unexpected element reach null/token validation, duplicate handling, projection identity buffering, quantity-evidence grouping, or exchange-result duplicate-to-ambiguity mutation before the stronger collection-integrity violation won.

## Contract

The affected boundaries are:

1. projection dimensions;
2. projection provenance;
3. projection sets;
4. quantity-evidence sets;
5. exchange-result sets.

For each boundary:

1. retain pre-enumeration validation of negative, conflicting, and over-limit known Count values;
2. before semantic processing of each yielded item, reject when `observedCount >= knownCount`;
3. retain the independent 10,000-item streaming/resource bound for sources without a known Count;
4. preserve null, token, duplicate, sorting, grouping, ambiguity, and identity behavior for items that are inside the declared cardinality;
5. after traversal, retain the final equality check so under-yield remains fail-closed.

`IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(...)` is the shared pre-item guard. It does not replace the existing per-collection Count discovery, capacity validation, or final mismatch checks.

## Deterministic regression

`IfcRoundTripKnownCountEarlyOverrunSmoke` is self-registering and proves:

- dimension over-yield wins before unexpected null processing;
- provenance over-yield wins before unexpected token processing;
- projection-set over-yield wins before unexpected identity processing;
- quantity-evidence over-yield wins before unexpected null/grouping processing;
- exchange-result over-yield wins before duplicate-external-identity mutation;
- under-yield still reaches the end of traversal and then fails the final Count equality check;
- honest counted inputs retain canonical dimension/provenance ordering, quantity-evidence grouping, and exchange-result acceptance.

The counting test collection records `MoveNext` calls so the over-yield cases prove that processing stops on the first unexpected yielded item rather than merely observing the right exception after later traversal.

`scripts/preflight-ifc-roundtrip-known-count-early-overrun.py` locks the five pre-item guards, their ordering before semantic processing, existing final mismatch checks, streaming bounds, duplicate semantics, and smoke registration.

## Validation boundary

This is Core-only IFC round-trip/data-integrity hardening. It does not change IFC schema semantics, quantity arithmetic, native BricsCAD adapters, external-format coverage, or persistence. No licensed BricsCAD host, private DWG, signing evidence, or `LOCAL_PASS` applies.

Landing requires deterministic Core smoke plus protected current-candidate `preflight` and `core` success under the repository CI contract.
