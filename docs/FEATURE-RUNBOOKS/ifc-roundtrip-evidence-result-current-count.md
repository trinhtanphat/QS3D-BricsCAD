# IFC round-trip evidence/result traversal Count integrity

## Scope

This contract covers the two IFC round-trip collection boundaries that materialize quantity evidence and exchange results:

- `IfcRoundTripQuantityEvidenceSet.Create`
- `IfcRoundTripExchangeResultSet.Create`

These inputs may be arbitrary caller-controlled `IEnumerable<T>` implementations. A supported deterministic `Count` surface is integrity evidence, not merely a capacity hint.

## Required ordering

Once Count is admitted, both builders must enforce this ordering for every traversal attempt:

```text
admit Count
  -> rebind Count
  -> MoveNext
  -> rebind Count
  -> known-count overrun / hard ceiling
  -> Current
  -> rebind Count
  -> null / identity / duplicate handling and staging
  -> final under-yield check
  -> post-traversal Count rebound
  -> canonical sort/group/dedup publication
```

`MoveNext` and `Current` are caller-controlled code. Either may mutate a counted source. A transient Count drift must be rejected at the first boundary where it becomes observable; it must not be allowed to restore before final validation. A `Current`-induced drift must win before the returned item can enter ordinary null/identity/duplicate validation or staged export state.

## Preserved behavior

The hardening does not change the established 10,000-item ceilings, early known-count overrun rejection, under-yield validation, negative/conflicting Count handling, canonical quantity-evidence grouping, exchange-result duplicate semantics, deterministic sorting, or pure-streaming support. Inputs that expose no supported Count contract remain streaming sources and are not converted into counted collections.

## Deterministic evidence

`IfcRoundTripEvidenceResultCurrentCountSmoke` covers both collection boundaries with two hostile counted behaviors:

1. `MoveNext` temporarily changes Count. The builder must fail before any `Current` read, proving transient drift cannot repair itself later.
2. `Current` changes Count while returning a null item. The canonical Count-change error must win before ordinary null-item validation.

Stable counted and pure-streaming controls remain accepted.

`scripts/preflight-ifc-roundtrip-evidence-result-current-count.py` pins the source ordering and required regression controls so later refactors cannot silently move item acceptance ahead of a Count rebound.

## Runtime boundary

No licensed BricsCAD or private DWG runtime is required. This is deterministic Core/export integrity and is accepted through repository source guards, Core build and smoke validation.
