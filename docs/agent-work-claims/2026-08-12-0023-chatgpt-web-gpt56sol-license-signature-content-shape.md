# Work claim — license signature content shape

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:23:00+07:00`
- Baseline main SHA observed before reservation: `42ad446c6d70ba4462e4c830e83d16733aa368e1`
- Priority: continue-all remote-safe signed-license parser integrity

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Confirmed defect

`LicenseVerifier.Load(...)` read the persisted `<signature>` body through `XElement.Value`. That API concatenates descendant text, so a structurally unsupported nested element such as `<signature algorithm="RSA-SHA256"><shadow>AA==</shadow></signature>` was silently flattened into `AA==` and accepted as signature bytes. The signed-license schema must fail closed instead of reinterpreting nested XML markup as the cryptographic signature payload.

## Implemented fix

- Reject any `<signature>` element with nested child elements before reading its text or decoding Base64.
- Preserve the existing text-only `RSA-SHA256` signature path.
- Preserve the existing root namespace, child-cardinality, DTD, token-whitespace and signature-size behavior.

## Focused regression

`NestedSignatureMarkupIsRejected()` writes a license whose nested child contains valid Base64 (`AA==`) and requires `LicenseVerifier.Load(...)` to reject it. This proves the guard is about unsupported XML shape rather than malformed Base64.

## Completion evidence

- Claim commit: `14cde59189e772e068f27aeb2a0e05ac0b83d059`
- Source fix: `662d7dd84e45c6063456d182ace10abeb3c69b24`
- Focused regression: `c91b45c2b83d71def6d6f8b72102390a68ec64c2`
- Source re-read from current `main`: confirmed `signatureElement.HasElements` rejection precedes `signatureElement.Value` / Base64 decoding.
- Regression re-read from current `main`: confirmed `Run()` executes `NestedSignatureMarkupIsRejected()` and the nested payload uses valid Base64 descendant text.
- Combined commit statuses for the regression commit: none reported.
- PR-triggered workflow runs for the regression commit: none reported, consistent with the repository's manual-only workflow policy.
- Executable Core smoke was not run in this remote container because `dotnet` is unavailable here.
- BricsCAD V25 runtime qualification was not performed or claimed.

## Explicit exclusions

- No root namespace, child-cardinality or token-whitespace changes already completed by earlier licensing claims.
- No SKU, trial, subscription, seat, machine, entitlement, backend or key-rotation policy.
- No updater, installer or release-policy changes.
- No BricsCAD V25/native/UI/runtime work.
- No GitHub Actions dispatch.

## Validation boundary

Source review plus focused Core smoke regression source only. There is no executable smoke or BricsCAD V25 runtime PASS claim from this remote session.
