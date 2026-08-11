# Work claim — license token canonical whitespace

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-token-canonical-whitespace`
- Registered: `2026-08-11T22:08:00+07:00`
- Completed: `2026-08-11T22:18:00+07:00`
- Baseline main SHA: `34b871b659c4e7ee87a5d0bc9076367d4ac1b6af`
- Reservation commit: `24f623ab2e01a78dc2c9ae7e948a83f42eca468b`
- Priority: close a deterministic signed-license canonicalization defect without inventing commercial licensing policy.

## Defect fixed

`LicenseDocument.ValidateToken(...)` previously accepted nonblank identifiers/features with leading or trailing whitespace, so an in-memory document could be signed with values such as `" LIC-001 "` or `" quantity "`. `LicenseVerifier.Load(...)` canonicalizes XML attributes through `Required(...).Trim()` before verification, so those values could not round-trip through the supported loader without changing the signed bytes.

`ValidateToken(...)` now rejects any token whose value differs from `Trim()`. Signed in-memory scalar identifiers and feature tokens therefore use the same canonical whitespace representation that persisted XML produces.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Delivered contract

- `LicenseId`, `CustomerId`, `ProductId`, `Nonce` and every feature must already equal their trimmed form before canonical payload generation/verification.
- Existing valid tokens, feature delimiter rejection, signature verification and validity-window behavior remain unchanged.
- XML loading continues to trim attribute text before constructing the in-memory document, preserving compatibility for ordinary persisted licenses.

## Published commits

- `10438bbc3b2c9e6ba53011d37cac3c2bf2e3f65e` — require canonical token whitespace in `LicenseDocument.ValidateToken(...)`.
- `4c3cccb38d26f95b361cc2715dcf544252113e0e` — conflict-safe merge of focused scalar/feature whitespace smoke coverage from PR #493.

## Validation notes

- Focused regression covers a leading-whitespace scalar, trailing-whitespace scalar and padded feature, while the existing successful signed-license case remains in the same registered smoke suite.
- Exact target blobs were re-read before writes. Stale write/ref races were rejected by GitHub; no force-push was used.
- The current execution environment has `git` but no `dotnet` or `gh`, so Core smoke/build execution was not performed in this session.
- GitHub Actions were not dispatched; repository CI remains manual-only.

## Excluded scope

- No SKU/trial/subscription/seat/machine/offline-grace/backend/key-rotation policy.
- No production signing credentials, updater, installer or release changes.
- No BricsCAD V25/native/UI/runtime work.

## Completion condition

Satisfied for the source/static contract. Exact executable smoke/build evidence remains separate from this remote source change.
