# Regeneration work profile Current Count integrity

## Scope

`RegenerationWorkProfile` materializes target ids, work items, and category summaries into immutable DTO collections. These inputs may be arbitrary `IEnumerable<T>` implementations, including collections that advertise Count through generic, read-only-generic, or non-generic collection interfaces.

This contract is Core-only. No licensed BricsCAD runtime or private DWG is required for acceptance.

## Threat model

A caller-controlled enumerator may mutate its advertised Count from either `MoveNext()` or `Current`. A Count mutation from `Current` is especially dangerous because the enumerator can simultaneously return an apparently valid value. If the materializer validates or retains that value before rebinding Count, stale or contradictory cardinality evidence crosses the DTO publication boundary.

## Fail-closed traversal contract

For a counted input, `MaterializeBounded<T>` must preserve this ordering:

1. admit and reconcile generic/read-only/non-generic Count evidence;
2. rebind Count immediately before traversal starts;
3. call `MoveNext()`;
4. on success, rebind Count before capacity/overrun decisions;
5. reject known-count overrun and project-element ceiling before reading overflow `Current`;
6. detach `Current` exactly once;
7. immediately rebind all admitted Count interfaces;
8. only then validate nullability and retain the item;
9. after terminal `MoveNext`, validate under-yield and rebind Count again before immutable publication.

There is no caller-controlled source operation between successful post-`Current` validation/retention and the next `MoveNext`, so a redundant per-iteration pre-`MoveNext` Count read is not required. This keeps the established stable one-item Count observation budget at five reads while adding the missing Current boundary.

Pure streaming inputs continue to use the project-element ceiling and do not acquire an artificial Count contract.

## Deterministic acceptance

`RegenerationWorkProfileCurrentCountIntegritySmoke` proves:

- target Count growth caused by `Current` is rejected immediately;
- work-item Count shrink caused by `Current` is rejected immediately;
- category Count becoming negative from `Current` is rejected immediately;
- cross-interface Count disagreement introduced by `Current` is rejected;
- stable counted input remains accepted with the five-boundary observation budget;
- pure streaming input remains accepted inside the project-element ceiling.

The focused source guard pins the positional ordering. The historical known-count stability guard remains active and is strengthened to understand the explicit post-`Current` checkpoint rather than weakening any prior overrun, under-yield, transient-MoveNext, ceiling, or final-publication requirement.

## Validation

Repository-safe acceptance is fresh exact-head Shared CI with protected `preflight + core`. If protected `main` advances, reconcile non-force, confirm path reservations/collision state, and obtain fresh exact-candidate checks before expected-head merge.
