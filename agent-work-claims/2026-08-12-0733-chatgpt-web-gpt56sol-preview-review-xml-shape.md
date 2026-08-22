# Work claim — Preview Review snapshot strict XML shape

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:33:00+07:00`
- Baseline main SHA: `5cbebc2981a263a220f6c50df5aa5d6f4e872bb7`
- Priority: P1 — persisted review snapshot integrity / fail-closed parsing.

## Reserved scope

Harden `PreviewReviewSnapshotStore.Load()` so persisted Preview Review XML must match the fixed shape emitted by the current serializer instead of silently ignoring namespaced/unknown/duplicate structure that is not represented in the canonical snapshot fingerprint.

## Expected surfaces

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs` — strict XML element/attribute/container/node shape validation before semantic parsing.
- One focused existing/isolated Core smoke source covering malformed persisted XML shapes while preserving normal save/load behavior.

## Required behavior

- Root must be the unnamespaced `qs3dPreviewReview` element with exactly the serializer-defined required attributes and no unsupported attributes.
- Exactly one unnamespaced `targets` and one unnamespaced `entries` container must exist; unknown/duplicate root children fail closed.
- `targets` contains only unnamespaced `target` elements with exactly the `id` attribute and no nested/unsupported content.
- `entries` contains only unnamespaced `entry` elements with exactly the serializer-defined entry attributes and no nested/unsupported content.
- Unsupported namespaces, attributes, element children, comments/processing instructions/non-whitespace mixed content fail closed.
- Snapshot/fingerprint canonicalization, numeric semantics, ordering rules and file format version remain unchanged.

## Excluded scope

- Preview generation/review business logic, revision report math, fingerprint algorithm/canonical payload, source-handle resolution, V25/V26 UI/runtime behavior.
- Licensing XML, QSDB persistence/audit, XLSX, rebar, Grid, health and other current ACTIVE/BLOCKED lanes.
- GitHub Actions and licensed BricsCAD qualification.

## Validation plan

- Re-fetch current `main`, exact source/test blobs and re-scan claims after this reservation lands.
- Add strict shape validation before `Load()` consumes known values.
- Extend or add an already registered isolated smoke using a valid saved snapshot mutated into malformed XML cases.
- Review exact source/regression diffs and close this claim with exact commit evidence.
- No Actions dispatch; no runtime/build PASS claim without execution evidence.
