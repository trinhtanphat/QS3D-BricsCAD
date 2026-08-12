# Work claim — Preview Review XML text safety

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:55:00+07:00`
- Baseline main SHA observed: `0ec378d42acdf2d62b031f7dd011040e2914f78f`
- Priority: P1 persisted Preview Review serialization safety
- Task Key: `CORE-PREVIEW-REVIEW-XML-TEXT-SAFETY`

## Confirmed defect

`PreviewReviewSnapshotService.Verify(...)` validates canonical IDs, fields, category/change semantics, summary counts and fingerprint consistency, but it does not validate that all strings later persisted as XML attributes contain XML-valid characters. `PreviewReviewSnapshotStore.Save(...)` calls `Verify(...)` before path/directory/temp-file work, then constructs `XAttribute` values. A snapshot can therefore be accepted as valid in memory and only fail later during XML serialization when, for example, the review name or entry provenance contains an XML-invalid control/surrogate character.

The repository has just hardened Revision capture against the same XML serialization class of defect. Search found no Preview Review XML-text safety claim or fix. The completed Preview Review change-domain lane owns only the `Added` / `Changed` / `Removed` value domain and is closed.

## Reserved scope

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs` — persisted-string XML character validation through the existing snapshot invariant boundary only.
- `tests/QS3D.Core.SmokeTests/PreviewReviewXmlTextSafetySmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Intended contract

- Every string serialized by `PreviewReviewSnapshotStore` must pass `XmlConvert.VerifyXmlChars(...)` as part of `ValidateSnapshot(...)` / `Verify(...)`.
- Reject XML-invalid in-memory snapshot state before Save performs path/directory/temp-file work.
- Preserve all XML-valid text exactly, including XML-valid tab/newline/carriage-return characters where current semantics allow them.
- Preserve fingerprint computation, format/schema, category/change/field rules, query/comparison semantics and loader XML-shape behavior.

## Validation plan

- Re-fetch moving `main` and exact PreviewReviewSnapshot blob after claim.
- Add one shared XML-text invariant helper and cover every persisted snapshot/entry string.
- Add focused producer-driven smoke for XML-invalid review name and provenance payload plus valid XML whitespace round-trip.
- Read back exact source/test diffs and verify ancestry.
- No GitHub Actions dispatch; no executable .NET/full build or BricsCAD V25/V26 runtime PASS claim without actual execution.
