# Rebar stock-demand transient Count stability

Carrier: #4976 / `issue-4976`

`RebarStockDemand` treats any supported `requiredCuts` Count as admission evidence. Because `MoveNext` and `Current` are caller-controlled boundaries, the admitted Count must be rebound immediately after each boundary and before the cut is semantically accepted or retained.

Required ordering for each yielded cut is:

`MoveNext -> Count rebound -> admitted-count overrun / 10,000 bound -> Current -> Count rebound -> null/identity checks -> retention/arithmetic`.

The constructor must continue to fail closed for under-yield, over-yield, conflicting collection-interface Counts, negative/oversized known Counts, duplicate cut identities, non-finite arithmetic, and traversal-wide Count changes. Stable ordinary lists remain supported.

Deterministic regression coverage in `RebarStockDemandSmoke` uses hostile `IReadOnlyList<RebarCutRequirement>` implementations whose Count changes transiently inside `MoveNext` or `Current` and then recovers. Both cases must fail before a hostile cut can be retained. This is a Core-only contract; no licensed BricsCAD runtime evidence is required.
