# Curtain frame/opening transient known-Count stability

Lane-Key: `issue-4617`

## Scope

`CurtainFrameOpeningPlanner.Interrupt` materializes caller-controlled frame and opening enumerables before deterministic subtraction. Completed issue-4517 established Count admission, N+1 no-`Current`, exact cardinality, post-traversal Count rebinding, and streaming compatibility. This follow-up closes the remaining transient-stability gap between admission and final rebound.

Runtime is `NOT_APPLICABLE`; this is deterministic Core geometry integrity and does not require licensed BricsCAD evidence.

## Required traversal contract

For both `frames` and `openings`, once any supported `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection` Count evidence is admitted, its value and supported-interface source set are immutable for the entire materialization.

Each caller-controlled iteration must preserve this order:

1. rebind all supported Count surfaces before `MoveNext`;
2. execute `MoveNext`;
3. when `MoveNext` succeeds, rebind all supported Count surfaces again;
4. apply admitted-Count overrun and independent safety-cap checks;
5. only then dereference `IEnumerator.Current` and validate/materialize the item;
6. preserve exact under-yield validation and final Count rebound before subtraction/publication.

Transient growth, shrink, negative/oversized evidence, conflict, or source-set drift must fail closed at the first observed boundary. Count drift detected after a successful `MoveNext` must win before the corresponding `Current`; Count drift detected before the next iteration must win before the next caller-controlled `MoveNext`.

## Deterministic regression evidence

`CurtainFrameOpeningKnownCountIntegritySmoke` retains issue-4517 coverage and adds hostile sources proving:

- frame Count growth immediately after successful `MoveNext` fails with `MoveNextCalls == 1` and `CurrentReads == 0`;
- opening Count becomes negative immediately after successful `MoveNext` and fails with zero `Current` reads;
- opening Count shrinks after the first retained item and fails before the second `MoveNext`, with exactly one `MoveNext` and one `Current` read.

Stable multi-interface counted sources and pure streaming sources remain accepted.

## Guards

The historical `scripts/preflight-curtain-frame-opening-known-count-integrity.py` continues to pin the full known-Count contract and now locks the two traversal-boundary rebinds.

The auto-discovered `scripts/preflight-curtain-frame-opening-transient-count-stability.py` specifically prevents regression to `while (enumerator.MoveNext())` and requires hostile transient Count evidence for both input classes.

## Landing contract

Require exact-head Shared branch CI, non-force reconciliation with latest protected `main` if it advances, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge, and exact protected-main verification. Hosted CI is source evidence only; no licensed `LOCAL_PASS` is claimed.
