# Work claim — Preview Review structured field payload canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-preview-review-structured-field-payload`
- Registered: `2026-08-12T09:55:00+07:00`
- Last Updated: `2026-08-12T10:01:00+07:00`
- Baseline main SHA: `eea7eefdb45e7548be7b1abdd06d7a690ac0dbf5`
- Claim commit: `802b7e7b4e870ac4db61b29a166b8637f9546cd7`
- Implementation PR: `#732`
- Main implementation commit: `52460c04cb2a5cbf8b16d38a6e7b20de064ef193`
- Priority: evidence-driven Preview Review row-identity canonicality found during owner-requested `continue all`
- Task Key: `REVIEW-PREVIEW-STRUCTURED-FIELD-PAYLOAD-CANONICALITY`

## Confirmed defect

The completed Preview Review outer-field canonicality contract rejected surrounding whitespace on the full `field` string, but writer-owned structured fields contain a second semantic identity component after a prefix. Current writers create `Quantity:` plus a canonical non-empty output name and `Property:` plus a canonical non-empty revision property key.

Before this fix, values such as `Quantity: Cost` and `Property: WidthM` had no surrounding whitespace on the full field string, so they passed the outer canonicality check. `ProjectInterchangeElementPropertyPolicy.IsPortable(...)` also trims the Property suffix before portability classification, so a padded property payload could still be treated as portable. Such persisted values could become row identity and field facets even though current writers cannot emit them.

## Implemented scope

`PreviewReviewSnapshotService.IsCanonicalOptionalReviewField(...)` now composes:

- the existing exact-empty-or-trim-canonical outer token contract;
- a non-empty, nonblank, trim-canonical payload contract for writer-owned `Property:` fields;
- the same payload contract for writer-owned `Quantity:` fields.

The existing Category contract was separated onto the generic optional-token helper so Category does not accidentally inherit field-specific structured syntax. Exact-empty regeneration placeholder fields and unrelated unstructured review fields remain valid under their previous contracts.

The Quantity writer now reuses a `QuantityFieldPrefix` constant; emitted text remains exactly `Quantity:<output>`.

## Regression source

Extended `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs` with persisted XML tamper cases for:

- `Quantity: Cost`;
- `Property: WidthM`;
- both must fail through the existing canonical field boundary before fingerprint fallback.

Existing canonical `Quantity:Cost`, outer field canonicality, Category canonicality, portability and regeneration placeholder coverage remain intact.

## Surfaces changed

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs`
- this claim file

## Coordination / exclusions preserved

- Completed outer Field canonicality from PR `#705` and Category canonicality from PR `#716` remain intact.
- `RevisionService`, portability policy, Query/Comparison implementation, row-key algorithm, XML shape, snapshot format/fingerprint and UI/native surfaces were not modified.
- No structured syntax was imposed on unrelated unstructured fields.
- No GitHub Actions/build/release workflow was dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Validation evidence

- Claim was visible on `main` before source edits at `802b7e7b4e870ac4db61b29a166b8637f9546cd7`.
- Post-claim/current-main source and smoke blobs remained `88498c55ff45b91e96c9a6ffecfd969896b3a1e4` and `7c728cd51e2b83bdedc848294c40a1784d4d2465`, confirming no overlap before integration.
- Branch compare against its current-main baseline showed exactly two changed files: source `+21/-4`, smoke `+7/-0`.
- PR `#732` exact unified diff was reviewed before merge; the four production deletions were only the prior helper implementation being refactored into generic optional-token plus structured-payload helpers.
- Server-side squash merge with exact reviewed head `809a141fa48144f60c32faafdcbeea3951a46b1a` produced `52460c04cb2a5cbf8b16d38a6e7b20de064ef193`.
- Post-merge read-back shows source blob `16a479db176938f32b6ad87421f8c6f50ce48fbf` and smoke blob `f70c2537bd70deac943af9b0082e395671b4ef2d` with the intended structured payload guards/regression.
- Local executable smoke/build was not run or claimed in this connector-only environment.

## Completion

`COMPLETED`: current `main` rejects noncanonical payloads inside writer-owned `Quantity:` / `Property:` Preview Review fields while preserving exact-empty placeholders, Category semantics, portability and all prior Review artifact invariants.