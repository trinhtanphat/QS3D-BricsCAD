# Work claim — Generated Grid Annotation handle-list canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-handle-list-canonicality`
- Registered: `2026-08-12T09:59:00+07:00`
- Completed: `2026-08-12T10:03:00+07:00`
- Baseline main SHA: `2808d90412f298dee0e008a7806a7e898c360366`
- Priority: P1 — generated Grid Annotation handle-list metadata must preserve the writer-owned delimiter/spacing contract.
- Task Key: `CORE-GRID-ANNOTATION-HANDLE-LIST-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` persists `GeneratedGridAnnotationHandles` as `string.Join(";", generatedHandles)`, so writer-owned list tokens have no surrounding whitespace. Health previously trimmed each stored token before validation, allowing malformed persisted text such as `"A; B;C;D;E;F"` to pass without canonicality evidence.

## Implemented

- Claim: `1e90ad678e4cae03e08b910757d43de1000df58a`
- Branch source: `4e43f498236fcb155ed753b285bfaf1f55d6de9e`
- Branch smoke / reviewed PR head: `f0a0dc80064ad9285f300d30d26e7727d7210ed8`
- PR: `#731`
- Squash merge on `main`: `15b4e5c37e0d8401d4f2cba1d9d75cbbb0df9802`

`GeneratedGridAnnotationHealthService` now emits one `GRID_ANNOTATION_HANDLE_LIST_NON_CANONICAL` error when all tokens are non-empty but the raw list differs from the delimiter-only list reconstructed from trimmed tokens. Existing empty-token, duplicate, non-hex, source-overlap and count checks continue on trimmed values; hex-letter casing and handle order are unchanged.

## Regression coverage

`GeneratedGridAnnotationHandleListCanonicalitySmoke` covers padded tokens, exact canonical control, empty-token precedence, padded duplicate preservation and lowercase-hex spacing control.

## Validation

- Read back current provider and focused smoke from merged `main`.
- Compared squash merge `15b4e5c37e0d8401d4f2cba1d9d75cbbb0df9802` to later `main` `41349897d33edfbcdb374fe752d36e6fbb5909f5`: status `ahead`, `ahead_by=3`, `behind_by=0`, merge base exactly the squash commit; later changes were unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
