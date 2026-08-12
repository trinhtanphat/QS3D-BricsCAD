# Work claim — License canonical payload Unicode integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:56:00+07:00`
- Completed: `2026-08-12T08:00:00+07:00`
- Baseline main SHA: `c64fe9222cf12583084b26a9be7f0807b7bedc5f`
- Priority: evidence-driven remote-safe licensing canonicalization integrity

## Reason

`LicenseDocument.ValidateToken()` checked required/canonical whitespace, length and control characters, but did not reject malformed UTF-16 such as unpaired surrogates. `CanonicalPayload()` then encoded with replacement-fallback UTF-8, allowing distinct malformed token strings to collapse to replacement-character bytes before RSA signing/verification.

## Changed scope

License scalar/feature tokens must now be well-formed Unicode / strict-UTF8 encodable, and canonical payload generation uses the same strict UTF-8 encoder. Payload text format, field ordering, sorting, delimiters, RSA algorithm/status behavior, XML loader behavior for valid text and existing token bounds remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs` (`LicenseDocument` token/UTF-8 canonicalization only)
- `tests/QS3D.Core.SmokeTests/LicenseCanonicalPayloadUnicodeSmoke.cs`
- this claim file

## Excluded scope

- No signature algorithm/key/status/validity-window changes.
- No XML shape/cardinality/namespace changes.
- No normalization of valid Unicode text or case/culture policy changes.
- No native/UI changes, GitHub Actions dispatch or BricsCAD runtime claim.

## Completion

- Claim commit: `b0b1bd48941be14d0ad8e6f177232e37242f91a2`.
- Implementation commit: `70931ece22a586ae382046821687a41815e63a18` — validate every canonical scalar/feature token with strict UTF-8 and encode the signed payload with that same strict encoder.
- Regression commit: `1cc047373846d9a7dd9b79b934b70d28908ccc39` — cover malformed high/low surrogate scalar and feature rejection plus valid supplementary Unicode deterministic payload generation.
- Validation actually performed:
  - re-fetched current `LicenseDocument` and confirmed strict token validation and strict canonical-payload encoding;
  - re-fetched the dedicated smoke and confirmed malformed + valid supplementary Unicode cases are covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

Recent licensing XML-shape/token-canonicality and verification-result snapshot claims were completed before this lane. No overlapping current claim was found for malformed-Unicode handling in the signed canonical payload.

## Completion condition

Satisfied: current `main` fails closed on malformed Unicode before license canonical payload signing/verification, valid Unicode remains deterministic, focused regression coverage is present, and this claim is released as `COMPLETED`.
