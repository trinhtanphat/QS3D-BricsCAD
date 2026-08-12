# Agent Work Claim

Task key: CORE-PREVIEW-REVIEW-CDATA-NODE-SHAPE
Scope: `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`; focused smoke under `tests/QS3D.Core.SmokeTests/`
Status: ACTIVE
State: ACTIVE

## Defect

`PreviewReviewSnapshotStore` allows whitespace formatting via `node is XText`, but LINQ to XML `XCData` derives from `XText`. As a result, whitespace-only CDATA can pass both document-level and nested element shape validation even though the canonical serializer never emits CDATA. This makes a noncanonical XML node type load as a valid Preview Review artifact instead of failing closed.

## Boundaries

- Reject `XCData` before accepting ordinary whitespace `XText` at both document and element validation boundaries.
- Preserve ordinary whitespace formatting text, existing element/attribute grammar, fingerprint semantics, review field/category/change rules, query/comparison behavior, and file-size/DTD protections.
- Do not modify unrelated Review, Persistence, UI, or BricsCAD code.

## Verification

- Add focused auto-registered smoke coverage proving ordinary whitespace remains accepted while whitespace CDATA is rejected at document/root/container boundaries.
- Read back source/test and verify source/closure ancestry on current `main`.
- Smoke source commit/readback is not an executable test run; no .NET build, GitHub Actions, Python, or BricsCAD runtime PASS will be claimed unless actually executed.
