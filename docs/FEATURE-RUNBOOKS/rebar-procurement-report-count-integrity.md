# Rebar procurement report known-Count Current integrity

Lane-Key: `issue-4893`

## Contract

`RebarProcurementReportBuilder.Build` materializes caller-provided optimizer results before deterministic sorting. Supported collection Count surfaces are admission evidence and must remain stable for the entire traversal, including caller-controlled `IEnumerator.Current`.

For counted inputs the traversal order is:

1. bind a non-negative, non-conflicting Count no greater than 10,000 before enumeration;
2. rebind the admitted Count immediately before `MoveNext()`;
3. after a successful `MoveNext()`, rebind Count again;
4. reject known-count overrun and the independent streaming hard cap before reading `Current`;
5. read `Current`, then immediately rebind Count before null validation, duplicate-group mutation, summary construction, row mutation, or observed-count acceptance;
6. reject known-count under-yield and final Count drift after traversal.

Canonical ordering is `Count -> MoveNext -> Count -> bounds -> Current -> Count -> semantic acceptance -> terminal Count -> cardinality`.

This prevents an advertised N+1 item or transient Count growth, shrink, negative value, cross-interface conflict, or Current-induced Count drift from being semantically consumed. Inputs without a supported Count surface remain valid streaming inputs but are still bounded to 10,000 report rows.

## Preserved report behavior

The hardening does not change canonical `RebarProcurementSummary` projection, duplicate group-id rejection, null-result rejection, deterministic group/grade/diameter/stock-length sorting, optimizer semantics, or downstream CSV ownership.

## Deterministic regression

`RebarProcurementReportCountIntegritySmoke` uses adversarial counted sources while separately instrumenting `MoveNext` and `Current`. It verifies:

- advertised Count=1 with two yielded results rejects after the second move but before the second `Current`;
- advertised Count=2 with one yielded result rejects under-yield;
- transient growth, shrink, negative Count, and cross-interface conflict after the first successful move reject with one `MoveNext` and zero `Current` reads;
- a hostile `IReadOnlyCollection` whose `Current` mutates Count while returning null is rejected by Count integrity before ordinary null-result validation;
- stable counted and pure-streaming controls remain accepted.

`preflight-rebar-procurement-report-count-integrity.py` pins the exact production ordering from admission through final cardinality and the hostile-Current regression contract.

## Acceptance boundary

Runtime classification is `NOT_APPLICABLE`. This is deterministic Core/rebar data-integrity work. No licensed BricsCAD host or private DWG is required, and no `LOCAL_PASS` is claimed.
