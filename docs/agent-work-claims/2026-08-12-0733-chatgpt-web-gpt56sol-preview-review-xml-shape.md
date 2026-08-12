# Work claim — Preview Review snapshot strict XML shape

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:33:00+07:00`
- Completed: `2026-08-12T07:37:00+07:00`
- Baseline main SHA: `5cbebc2981a263a220f6c50df5aa5d6f4e872bb7`
- Claim commit: `377a5234bb865422d16c51d43e2ce8c946608fc5`
- Source fix commit: `eb73f30cdf79140db64d25075a07e07d4c96b828`
- Regression commit: `84f1b34284c3d186ef14324963993b8825371638`
- Priority: P1 — persisted review snapshot integrity / fail-closed parsing.

## Reserved scope

Harden `PreviewReviewSnapshotStore.Load()` so persisted Preview Review XML must match the fixed shape emitted by the current serializer instead of silently ignoring namespaced/unknown/duplicate structure that is not represented in the canonical snapshot fingerprint.

## Implemented surfaces

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs` — strict XML element/attribute/container/node shape validation before semantic parsing.
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs` — extended existing registered smoke with persisted malformed-shape cases.

## Implemented behavior

- `Load()` now validates the parsed `XDocument` shape before reading format, semantic attributes, targets, entries, or verifying the fingerprint.
- Root must be the unnamespaced `qs3dPreviewReview` element with exactly the serializer-defined required attributes.
- Exactly one unnamespaced `targets` and one unnamespaced `entries` container are required.
- `targets` accepts only unnamespaced `target` elements with exactly `id`.
- `entries` accepts only unnamespaced `entry` elements with exactly `elementId`, `category`, `change`, `field`, `before`, `after`, `beforeProvenance`, and `afterProvenance`.
- Unsupported namespace names, attributes, child elements, comments/processing instructions and non-whitespace mixed node content fail closed.
- Existing fingerprint algorithm, canonical payload, semantic validation, numeric parsing, ordering and format version were not changed.

## Regression coverage

`PreviewReviewSnapshotSmoke.UnsupportedXmlShapeFailsClosed()` starts from a valid snapshot saved by the production store and independently mutates the persisted XML into:

- an unexpected root attribute;
- a namespaced root;
- a duplicate `targets` container;
- an unsupported child under `targets`;
- an unexpected attribute on an `entry`;
- an unsupported nested child under an `entry`;
- an XML comment inside the root payload.

Each malformed artifact must fail at the strict XML-shape boundary with `InvalidDataException`; the existing round-trip, tamper, handle-field and portability smoke cases remain intact.

## Excluded scope honored

- Preview generation/review business logic, revision report math, fingerprint algorithm/canonical payload, source-handle resolution and V25/V26 UI/runtime behavior were not changed.
- Licensing XML, QSDB persistence/audit, XLSX, rebar, Grid, health and other concurrent lanes were not touched.
- No local-only qualification item was introduced or modified.

## Coordination and validation actually performed

- The claim was published alone on `main` before implementation and observed as current head at `377a5234bb865422d16c51d43e2ce8c946608fc5`.
- Recent commit/claim history was rescanned; no competing Preview Review XML-shape owner was found.
- Historical Preview Review hardening for required attributes, enum-kind canonicality and portability was reviewed and preserved.
- Exact source diff for `eb73f30cdf79140db64d25075a07e07d4c96b828` was reviewed; it contains only the parse-boundary call plus strict shape helpers.
- Exact regression diff for `84f1b34284c3d186ef14324963993b8825371638` was reviewed; it only extends the existing registered Preview Review smoke.
- This connector-only pass did not execute the .NET smoke suite or licensed BricsCAD runtime qualification, so executable/runtime PASS is not claimed.
- GitHub Actions were not dispatched; no force-push, reset, or history rewrite was used.

## Completion condition

Satisfied for remote/source scope: Preview Review persisted XML now fails closed on unsupported/lossy shape before semantic parsing or fingerprint verification, focused registered regression source is pushed to `main`, and validation limitations are recorded truthfully.
