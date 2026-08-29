# Review workbook Count boundary Current no-overread

Issue: #4492  
Lane-Key: `issue-4492`  
Runtime: `NOT_APPLICABLE` — deterministic Core export/data-integrity contract.

## Defect boundary

`Qs3dReviewWorkbookExporter.Export` accepts caller-provided `IReadOnlyList<T>` inputs for QTO detail, QTO summary, clash, duplicate, and optional issue geometry data. It uses advertised `Count` values to establish workbook row admission, but historical downstream `foreach` traversal could observe an N+1 `Current` before any body-level guard could reject that item.

The advertised Count is therefore part of the export semantic boundary. Once `MoveNext()` reports an item beyond that boundary, the exporter must fail before observing caller-controlled `Current`.

## Required traversal contract

For each caller-provided counted list:

1. bind the advertised `Count`;
2. allocate the retained snapshot from that admitted cardinality;
3. call `MoveNext()`;
4. if the admitted cardinality is already retained, reject before `Current`;
5. otherwise read `Current` exactly once and retain the item;
6. after normal termination, require exact traversal cardinality;
7. re-read `Count` and fail closed if it drifted after traversal;
8. only pass the stable snapshot to QTO/clash/duplicate/geometry semantic validation and XLSX generation.

Existing Excel row limits and admitted-row validation remain unchanged. The correction prevents an inconsistent collection surface from smuggling unadmitted rows into workbook processing or exposing their `Current` values.

## Deterministic regression evidence

`Qs3dReviewWorkbookCountNoOverreadSmoke` uses an instrumented `IReadOnlyList<T>` and independently counts Count reads, `MoveNext` calls, and `Current` reads. It covers:

- Count=1 / yield=2: `MoveNext=2`, `Current=1`;
- Count=0 / yield=1: `MoveNext=1`, `Current=0`;
- Count=2 / yield=1: exact-cardinality under-yield rejection after terminal traversal;
- exact traversal followed by Count drift: fail closed after termination with no extra `Current`;
- stable Count=2 / yield=2: order retained, terminal `MoveNext` observed, exactly two `Current` reads, and Count rebound after traversal.

`scripts/preflight-review-workbook-count-no-overread.py` is auto-discovered by aggregate feature guards. It requires the shared snapshot ordering and verifies that all QTO, clash, duplicate, and geometry paths are routed through that boundary rather than directly through caller collections.

## Acceptance

Remote-safe acceptance requires the feature source guard, deterministic Core smoke, Core build, trusted V25 compile-reference validation, V25 plugin compile, and final build to pass on the exact reconciled candidate. No licensed BricsCAD/private-DWG `LOCAL_PASS` is required or claimed.
