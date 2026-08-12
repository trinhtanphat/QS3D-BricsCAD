# Work claim — Preview Review XML text safety

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:55:00+07:00`
- Completed: `2026-08-12T10:56:00+07:00`
- Baseline main SHA observed: `0ec378d42acdf2d62b031f7dd011040e2914f78f`
- Claim commit: `97b5d49f09a61aafcfa94da9e9b4d40e05f6be66`
- Source fix: `337b0b3dc6c5c1dcb3e0f913ad4436a01bf03331`
- Regression smoke: `c3b433262e77a06d8ed1b8f2bf78cc6fec4b3a51`
- Priority: P1 persisted Preview Review serialization safety
- Task Key: `CORE-PREVIEW-REVIEW-XML-TEXT-SAFETY`

## Confirmed defect

`PreviewReviewSnapshotService.Verify(...)` validated canonical IDs, fields, category/change semantics, summary counts and fingerprint consistency, but did not validate that all strings later persisted as XML attributes contain XML-valid characters. `PreviewReviewSnapshotStore.Save(...)` calls `Verify(...)` before path/directory/temp-file work, then constructs `XAttribute` values. A snapshot could therefore be accepted as valid in memory and only fail later during XML serialization when, for example, the review name or entry provenance contained an XML-invalid control/surrogate character.

The repository had just hardened Revision capture against the same XML serialization class of defect. Search found no Preview Review XML-text safety claim or fix before reservation. The completed Preview Review change-domain lane owns only the `Added` / `Changed` / `Removed` value domain and remained unchanged.

## Implemented contract

- The canonical fingerprint `Part(...)` boundary now calls `XmlConvert.VerifyXmlChars(...)` before accepting every string part.
- Fingerprint construction already covers every string serialized by the Preview Review store: header identity/scope values, target IDs, and all entry category/change/field/before/after/provenance strings.
- XML-invalid text now fails with `InvalidOperationException` during snapshot construction/verification, before Save reaches path/directory/temp-file work.
- XML-valid text is not normalized or rewritten; existing semantics and fingerprint content remain unchanged.
- Format/schema, category/change/field rules, query/comparison semantics and loader XML-shape behavior are unchanged.

## Regression coverage

`PreviewReviewXmlTextSafetySmoke` is auto-registered and producer-driven:

- XML-invalid `U+0001` in a review name is rejected during snapshot creation;
- XML-invalid `U+0001` propagated through real Quantity Rule provenance is rejected during Preview Review snapshot creation;
- XML-valid tab content in review name and rule provenance round-trips exactly through the real Preview Review Save/Load store.

## Validation

- Exact source readback for `337b0b3dc6c5c1dcb3e0f913ad4436a01bf03331` shows only eight additions inside fingerprint `Part(...)`; no unrelated source change was introduced by the full-file contents update.
- Exact regression readback for `c3b433262e77a06d8ed1b8f2bf78cc6fec4b3a51` shows one new focused 110-line smoke source.
- Compared source fix to observed current `main` `c3b433262e77a06d8ed1b8f2bf78cc6fec4b3a51`: `ahead_by=3`, `behind_by=0`, source fix is the merge base, and no later commit modified `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`.
- No GitHub Actions were dispatched. Smoke source was committed/read back but not executed from this connector-only session. No executable .NET/full build PASS and no licensed BricsCAD V25/V26 runtime PASS are claimed.

## Completion

`COMPLETED`: Preview Review no longer accepts an in-memory artifact as fingerprint-valid when one of its persisted text values cannot be serialized as valid XML.
