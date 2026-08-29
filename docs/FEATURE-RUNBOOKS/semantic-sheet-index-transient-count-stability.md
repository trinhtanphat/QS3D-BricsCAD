# Semantic sheet index transient known-Count stability

Lane-Key: `issue-4621`

## Scope

`SemanticSheetIndexBuilder.MaterializeBounded` consumes caller-controlled sheet enumerables before duplicate validation, sorting and publication. Prior coverage established Count admission, N+1 no-`Current`, exact cardinality, final Count rebound, streaming ceiling and semantic controls. This carrier closes the remaining transient Count-stability gap during traversal.

Runtime is `NOT_APPLICABLE`; this is deterministic Core documentation integrity.

## Traversal contract

For counted inputs, the admitted Count must remain stable for the entire materialization. Each iteration must:

1. re-read all supported Count surfaces before `MoveNext` and reject changed/invalid/conflicting evidence;
2. execute `MoveNext`;
3. after a successful `MoveNext`, re-read Count again before capacity guards and before `Current`;
4. reject admitted-Count overrun and the independent 10,000-sheet ceiling before dereferencing `Current`;
5. preserve exact under-yield validation and a final Count stability check after traversal.

Pure streaming inputs remain accepted and retain the independent safety ceiling.

## Deterministic regression

`SemanticSheetIndexTransientCountStabilitySmoke` uses hostile counted collections to prove:

- growth immediately after the first successful `MoveNext` fails with one `MoveNext` and zero `Current` reads;
- a negative Count immediately after successful `MoveNext` fails before `Current`;
- shrink after the first retained item fails before the next `MoveNext`, with exactly one `MoveNext` and one `Current` read.

The historical stability smoke remains registered and updates its Count-read expectations to reflect traversal-boundary rebinding while retaining overrun, under-yield, final drift, conflict, negative admission, streaming, null, sorting and duplicate-number controls.

## Landing

Require exact-head Shared CI, non-force reconciliation with latest protected `main` when necessary, protected PR `preflight` + `core` SUCCESS, expected-head merge and exact protected-main verification. Hosted evidence does not imply licensed BricsCAD `LOCAL_PASS`.
