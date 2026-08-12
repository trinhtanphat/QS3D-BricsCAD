# Work claim — semantic change review revision-id integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-change-review-id-integrity-20260812-0918`
- Registered: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `5c64031980be4330749781df2a131a459c755885`
- Priority: evidence-driven remote-safe revision review identity integrity

## Confirmed defect

`RevisionSnapshot` is a public mutable DTO. `SemanticChangeReviewBuilder.Build(...)` validates semantic element identity/payload through its own index plus `RevisionService.Compare(...)`, but it does not validate `before.Id` / `after.Id` before copying them into public `SemanticChangeReview.BeforeRevisionId` and `AfterRevisionId`. A caller can therefore mutate an otherwise-valid snapshot to a blank or padded revision id and obtain a review carrying identity that `RevisionService.Capture(...)` itself would reject.

## Reserved scope

- Validate both revision ids as required, nonblank, no-surrounding-whitespace identities before building the review.
- Preserve the exact canonical id strings in the returned review.
- Add focused Core smoke coverage for blank/padded direct DTO mutation and valid id preservation.

## Expected surfaces

- `src/QS3D.Core/Revisions/SemanticChangeReview.cs`
- one focused `QS3D.Core.SmokeTests` regression file with ModuleInitializer
- this claim file

## Excluded scope

- `RevisionService.cs`, `QuantityRevisionReport.cs`, snapshot store/schema/backup, compare semantics, element payload validation, UI/native runtime.
- No requirement that before/after revision ids be different; this lane only enforces the canonical identity contract already used by Capture.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- blank before id fails closed;
- padded after id fails closed;
- canonical ids are preserved exactly in a valid review;
- semantic change grouping/order/count behavior remains unchanged.

## Completion condition

Focused source/regression are merged to current `main`, remote source/test are re-read, and this claim is closed `COMPLETED` with exact integration evidence.
