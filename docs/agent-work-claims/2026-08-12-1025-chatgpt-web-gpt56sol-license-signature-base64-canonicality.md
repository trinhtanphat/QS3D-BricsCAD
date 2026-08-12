# Work claim — license signature Base64 canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-signature-base64-canonicality-20260812-1025`
- Registered: `2026-08-12T10:25:00+07:00`
- Baseline main SHA: `423368eb88a34e92e58a0a0afea7d50688d63fbc`
- Priority: P1 licensing fail-closed / deterministic signed-document representation.

## Confirmed defect

`LicenseVerifier.Load(...)` currently evaluates signature text with `Convert.FromBase64String((signatureElement.Value ?? string.Empty).Trim())`. `Convert.FromBase64String` ignores Base64 whitespace, and the explicit `Trim()` also accepts surrounding whitespace. The loader can therefore accept multiple XML text spellings for exactly the same signature bytes even though the rest of the license loader has been hardened to strict child/attribute/token/timestamp representation. Signature bytes themselves are not part of the signed payload, so strict representation must come from the loader rather than signature verification.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs` — signature decode only
- `tests/QS3D.Core.SmokeTests/LicenseSignatureBase64CanonicalitySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Decode the exact signature element text without pre-trimming.
- Re-encode decoded bytes via `Convert.ToBase64String` and require exact ordinal equality with the stored text.
- Preserve empty-signature load behavior (verification still reports `InvalidSignature`), maximum signature size, RSA-SHA256 algorithm policy, text-only node policy and all signed canonical payload fields.
- Add focused smoke proving canonical Base64 text loads unchanged while surrounding or embedded whitespace spellings fail closed.

## Coordination

Recent licensing child/attribute strictness lanes are completed. LOCAL-003 currently owns unrelated Core smoke fixture files only. No native/UI/persistence source is in scope.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD runtime PASS claimed.
