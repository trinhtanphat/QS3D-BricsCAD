# Work claim — Preview Review document-level XML node shape

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:41:00+07:00`
- Completed: `2026-08-12T07:45:00+07:00`
- Baseline main SHA: `a3376d05d0026e0beb7c99f23d4869b474e4c90d`
- Claim commit: `bb42dd7ac76880731ca89add594632d070be2f78`
- Source fix commit: `c3849ed39c3999c91aade89e65547c51657a34bd`
- Regression commit: `7e026c905b0c3372d913ede723977efe293bcbd5`
- Priority: P1 — complete fail-closed persisted Preview Review XML shape enforcement.

## Reserved scope

Close the remaining document-level gap after the completed Preview Review strict XML-shape hardening: reject unsupported `XDocument` nodes outside the single root element before semantic parsing/fingerprint verification.

## Implemented behavior

- `ValidateXmlShape(XDocument)` now enumerates document-level nodes before validating the root payload.
- The single parsed root element is accepted.
- Whitespace-only `XText` outside the root is accepted.
- Any comment, processing instruction, non-whitespace document text or other extra document-level node fails closed with `InvalidDataException`.
- XML declarations remain compatible because LINQ to XML represents the declaration separately from `XDocument.Nodes()`.
- Existing root/container/target/entry shape checks, fingerprint canonical payload, format version and semantic validation were not changed.

## Regression coverage

The existing registered `PreviewReviewSnapshotSmoke.UnsupportedXmlShapeFailsClosed()` now additionally mutates a valid production-saved artifact into:

- a document-level comment before the root;
- a document-level processing instruction after the root.

Both artifacts must fail at the Preview Review XML shape boundary. Existing malformed root/container/entry tests remain unchanged.

## Excluded scope honored

- Preview business logic, review comparison/query, fingerprint algorithm, V25/V26 UI/runtime and unrelated active lanes were not changed.
- No GitHub Actions, force-push, reset or history rewrite was used.

## Validation actually performed

- Re-fetched current source/test blobs after claim publication.
- Reviewed exact source commit `c3849ed39c3999c91aade89e65547c51657a34bd`; diff is only the document-node loop.
- Reviewed exact regression commit `7e026c905b0c3372d913ede723977efe293bcbd5`; diff only adds the two persisted document-level malformed cases.
- This connector-only pass did not execute the .NET smoke suite or licensed BricsCAD runtime qualification, so executable/runtime PASS is not claimed.

## Completion condition

Satisfied for remote/source scope: Preview Review strict XML shape now covers both the root payload and surrounding document-level nodes, focused regression source is pushed to `main`, and validation limits are recorded truthfully.
