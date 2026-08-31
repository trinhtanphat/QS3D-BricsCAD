# QS3D Review workbook transient Count stability

## Scope

`Qs3dReviewWorkbookExporter.SnapshotCounted` is the shared counted-input boundary used before QTO detail, QTO summary, clash, duplicate and issue-geometry rows are accepted into a QS3D Review workbook package.

## Integrity contract

For an admitted `IReadOnlyList<T>` Count, export must fail closed if the caller-controlled collection changes Count anywhere across traversal. The helper therefore rebinds the admitted Count immediately before and after every `MoveNext()`, after reading each admitted `Current` exactly once and before retaining that value, at terminal traversal, and again before returning the completed snapshot.

The existing over-yield contract remains fail-early: once the admitted item cardinality is reached, a successful next `MoveNext()` is rejected before reading the unexpected `Current`. Under-yield and final Count mismatch remain invalid. Initial negative Count remains invalid. Stable counted input preserves order and reads one `Current` per retained row.

For a stable two-item source, the focused smoke observes ten Count reads total: one caller admission read plus nine traversal/publication observations. A one-item Current-induced drift is observed on the fourth Count read, immediately after that Current and before `result.Add`.

## Deterministic regression

`Qs3dReviewWorkbookCountNoOverreadSmoke` covers:

- known Count overrun without reading the unexpected Current;
- zero-count overrun with zero Current reads;
- under-yield cardinality failure;
- Count drift exposed by `MoveNext()` before Current;
- Count drift induced by `Current` before retention;
- final/post-traversal Count drift;
- stable two-item order, one-Current-per-row and explicit Count observation budget.

`scripts/preflight-qs3d-review-workbook-count-stability.py` pins the source ordering so future refactors cannot move retention before the post-Current Count rebound or remove the traversal-wide checks while leaving tests superficially green.

## Runtime classification

This package is deterministic Core export/data-integrity work. Licensed BricsCAD/private-DWG runtime evidence is not applicable and must not be claimed as `LOCAL_PASS`.
