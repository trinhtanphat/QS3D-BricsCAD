# TBQ workspace transient known-Count stability

## Scope

This runbook covers caller-controlled enumerable materialization in `TbqProjectWorkspaceState` for bill items, build-up rates, rate references, and BQ library entries. It is deterministic Core/cost correctness work; no licensed BricsCAD runtime is required or claimed.

## Integrity contract

When an input exposes a supported known Count surface (`ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`), the admitted Count is a traversal-wide integrity contract rather than admission/final metadata only.

For every caller-controlled item boundary, TBQ must rebind all supported Count surfaces immediately before MoveNext and after successful MoveNext, before Current is read. Negative, conflicting, grown, or shrunk Count metadata fails closed before the affected item is materialized. A Count=N source that yields an N+1 item may execute the extra MoveNext needed to prove over-yield, but must reject before reading that N+1 Current.

The final post-traversal equality and Count rebind remain required. Pure streaming inputs without a supported Count surface remain valid and are bounded by the existing ceilings.

## Surfaces

- bill items: maximum 10,000;
- build-up rates: maximum 10,000;
- rate references: maximum 50,000;
- BQ library entries: maximum 10,000.

Rate references and BQ library entries pass through the TBQ bounded wrapper before their downstream DeepCost materializers. The wrapper itself must enforce the original caller's Count contract before yielding any `Current` value downstream.

## Preserved behavior

The hardening must preserve null/duplicate validation, deterministic bill/build-up sorting, exact known-count under/over-yield checks, stable counted inputs, pure streaming inputs, downstream rate-reference/library behavior, currency/CFA/ratio validation, and all existing limits.

## Deterministic validation

`TbqWorkspaceTransientCountSmoke` exercises hostile MoveNext-induced Count growth/shrink/negative/conflicting metadata across the four TBQ surfaces, verifies rejection before Current, verifies Count=N over-yield reads exactly N Current values, and keeps stable counted and pure-streaming controls.

`scripts/preflight-tbq-workspace-transient-count-stability.py` pins explicit traversal ordering and forbids regression to caller-controlled `foreach` on these paths.
