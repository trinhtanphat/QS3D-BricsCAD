# Work claim — Preview Review structured field payload canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-preview-review-structured-field-payload`
- Registered: `2026-08-12T09:55:00+07:00`
- Last Updated: `2026-08-12T09:55:00+07:00`
- Baseline main SHA: `eea7eefdb45e7548be7b1abdd06d7a690ac0dbf5`
- Priority: evidence-driven Preview Review row-identity canonicality found during owner-requested `continue all`
- Task Key: `REVIEW-PREVIEW-STRUCTURED-FIELD-PAYLOAD-CANONICALITY`

## Confirmed defect

The completed Preview Review field-canonicality lane rejects surrounding whitespace on the full `field` string, but structured review fields contain a second semantic identity component after a writer-owned prefix. Current writers produce `Quantity:` plus a canonical non-empty quantity output name and `Property:` plus a canonical non-empty revision property key. `RevisionService.Capture/Compare` validates property keys before creating `Property:<key>`, and Quantity Rule preview creation validates output names before creating `Quantity:<output>`.

The artifact validator currently accepts values such as `Property: WidthM`, `Property:WidthM ` cannot pass outer canonicality, but `Property: WidthM` does; likewise `Quantity: Cost`. These strings have no outer padding, so they pass `IsCanonicalOptionalReviewField(...)`. `ProjectInterchangeElementPropertyPolicy.IsPortable(...)` trims the property suffix before portability classification, so the padded property payload is also treated as portable. Such values can therefore become persisted row identity and field facets even though the current writer cannot emit them.

## Reserved scope

For writer-owned structured field forms `Property:<key>` and `Quantity:<output>`, require the payload after the prefix to be non-empty, nonblank and free of leading/trailing whitespace. Preserve exact-empty regeneration placeholder fields and existing unstructured fields. Reject malformed persisted structured fields rather than trimming/repairing them.

## Expected surfaces

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs`
- this claim file

## Explicit exclusions / coordination

- Preserve completed outer Field canonicality (`#705`) and Category canonicality (`#716`).
- No change to `RevisionService`, Quantity Rule output generation, portability policy, Query/Comparison behavior, row-key algorithm, XML shape, snapshot format/fingerprint or UI/native surfaces.
- Do not impose structured syntax on unrelated unstructured fields such as `Category`, `FamilyId`, `FloorId`, `ZoneId`, `Dependencies` or `SourceHandles`.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- Existing `Quantity:Cost` remains valid and round-trips.
- Existing canonical `Property:<key>` writer output remains valid.
- Persisted `Quantity: Cost` is rejected at the structured field boundary before fingerprint fallback.
- Persisted `Property: WidthM` is rejected even though the portability policy would otherwise trim the property suffix.
- Exact-empty regeneration placeholder remains valid.
- Re-fetch moving `main` source/test blobs and inspect exact PR diff before integration.

## Completion condition

Current `main` rejects noncanonical payloads inside writer-owned `Quantity:` / `Property:` Preview Review fields while preserving all completed Review artifact contracts, focused regression source is merged, and this claim is closed `COMPLETED` with exact evidence.