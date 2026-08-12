# Work claim — Preview Review document-level XML node shape

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:41:00+07:00`
- Baseline main SHA: `a3376d05d0026e0beb7c99f23d4869b474e4c90d`
- Priority: P1 — complete fail-closed persisted Preview Review XML shape enforcement.

## Reserved scope

Close the remaining document-level gap after the completed Preview Review strict XML-shape hardening: reject unsupported `XDocument` nodes outside the single root element (comments, processing instructions, non-whitespace text or additional document content) before semantic parsing/fingerprint verification.

## Expected surfaces

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs` — extend `ValidateXmlShape(XDocument)` only at document-node level.
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs` — extend the existing registered malformed-shape smoke with document-level comment/processing-instruction cases.

## Excluded scope

- Existing root/container/entry/target shape rules, fingerprint canonical payload, preview business logic, review comparison/query, V25/V26 runtime/UI, and all unrelated ACTIVE/BLOCKED lanes.
- GitHub Actions and licensed BricsCAD qualification.

## Validation plan

- Re-fetch exact current source/test blobs after claim publication.
- Require the document to contain exactly the root element plus ignorable whitespace/declaration representation permitted by LINQ; reject comment/PI/non-whitespace document nodes.
- Add persisted regression mutations before/after root.
- Review exact diffs and close claim with exact SHAs; no executable/runtime PASS claim without actual execution.
