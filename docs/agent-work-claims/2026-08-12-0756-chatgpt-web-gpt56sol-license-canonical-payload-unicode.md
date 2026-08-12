# Work claim — License canonical payload Unicode integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:56:00+07:00`
- Baseline main SHA: `c64fe9222cf12583084b26a9be7f0807b7bedc5f`
- Priority: evidence-driven remote-safe licensing canonicalization integrity

## Reason

`LicenseDocument.ValidateToken()` currently checks required/canonical whitespace, length and control characters, but does not reject malformed UTF-16 such as unpaired surrogates. `CanonicalPayload()` then encodes with the default `Encoding.UTF8`, whose replacement fallback can map distinct malformed token strings to the same U+FFFD bytes. Because this byte sequence is the payload that RSA signs/verifies, malformed semantic identity text can become lossy before cryptographic canonicalization instead of failing closed.

## Reserved scope

Require license scalar/feature tokens to be well-formed Unicode / strict-UTF8 encodable and encode the canonical payload with the same strict UTF-8 instance. Preserve payload text format, field ordering, sorting, delimiters, RSA algorithm/status behavior, XML loader semantics for valid text and all existing token bounds. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs` (`LicenseDocument` token/UTF-8 canonicalization only)
- `tests/QS3D.Core.SmokeTests/LicenseCanonicalPayloadUnicodeSmoke.cs`
- this claim file

## Excluded scope

- No signature algorithm/key/status/validity-window changes.
- No XML shape/cardinality/namespace changes.
- No normalization of valid Unicode text or case/culture policy changes.
- No native/UI changes, GitHub Actions dispatch or BricsCAD runtime claim.

## Validation plan

- Assert malformed unpaired high/low surrogates in scalar and feature tokens are rejected by validation/canonical payload generation.
- Assert two distinct malformed surrogate inputs are rejected rather than silently producing replacement-fallback canonical payload bytes.
- Assert valid supplementary Unicode represented by a proper surrogate pair remains accepted and deterministic in canonical payload generation.
- Re-fetch current full source immediately before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent licensing XML-shape/token-canonicality and verification-result snapshot claims are completed. No current/recent claim was found for malformed-Unicode handling in the signed canonical payload.

## Completion condition

Current `main` fails closed on malformed Unicode before license canonical payload signing/verification, valid Unicode remains deterministic, focused regression coverage is present, and this claim is marked `COMPLETED`.
