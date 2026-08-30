# Rebar procurement report known-Count Current integrity

## Contract

`RebarProcurementReportBuilder.Build` materializes caller-provided optimizer results before deterministic sorting. Supported collection Count surfaces are admission evidence and must remain stable for the entire traversal.

For counted inputs the traversal order is:

1. bind a non-negative, non-conflicting Count no greater than 10,000 before enumeration;
2. rebind the admitted Count immediately before `MoveNext()`;
3. after a successful `MoveNext()`, rebind Count again;
4. reject known-count overrun and the independent streaming hard cap before reading `Current`;
5. read and validate the result only after those cardinality checks;
6. reject known-count under-yield and final Count drift after traversal.

This prevents an advertised N+1 item or transient Count growth, shrink, negative value, or cross-interface conflict from being semantically consumed. Inputs without a supported Count surface remain valid streaming inputs but are still bounded to 10,000 report rows.

## Preserved report behavior

The hardening does not change canonical `RebarProcurementSummary` projection, duplicate group-id rejection, null-result rejection, deterministic group/grade/diameter/stock-length sorting, optimizer semantics, or downstream CSV ownership.

## Deterministic regression

`RebarProcurementReportCountIntegritySmoke` uses an adversarial collection implementing generic, read-only, and non-generic Count surfaces while separately instrumenting `MoveNext` and `Current`. It verifies:

- advertised Count=1 with two yielded results rejects after the second move but before the second `Current`;
- advertised Count=2 with one yielded result rejects under-yield;
- transient growth, shrink, negative Count, and cross-interface conflict after the first successful move reject with one `MoveNext` and zero `Current` reads;
- stable counted and pure-streaming controls remain accepted.

## Acceptance boundary

This is deterministic Core data-integrity work. No licensed BricsCAD host or private DWG is required, and no `LOCAL_PASS` is claimed.
