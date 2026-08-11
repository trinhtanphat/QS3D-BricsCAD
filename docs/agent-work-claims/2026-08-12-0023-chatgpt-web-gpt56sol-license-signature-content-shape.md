# Work claim — license signature content shape

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:23:00+07:00`
- Baseline main SHA observed before reservation: `42ad446c6d70ba4462e4c830e83d16733aa368e1`
- Priority: continue-all remote-safe signed-license parser integrity

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Confirmed defect

`LicenseVerifier.Load(...)` reads the persisted `<signature>` body through `XElement.Value`. That API concatenates descendant text, so a structurally unsupported nested element such as `<signature algorithm="RSA-SHA256"><shadow>AA==</shadow></signature>` is silently flattened into `AA==` and accepted as signature bytes. The signed-license schema must fail closed instead of reinterpreting nested XML markup as the cryptographic signature payload.

## Planned regression

- Preserve normal text-only `RSA-SHA256` signature parsing.
- Reject a `<signature>` element containing a nested child element even when its descendant text is valid Base64.
- Preserve existing root namespace, section-cardinality, DTD, token-whitespace and signature-size behavior.

## Explicit exclusions

- No root namespace, child-cardinality or token-whitespace changes already completed by earlier licensing claims.
- No SKU, trial, subscription, seat, machine, entitlement, backend or key-rotation policy.
- No updater, installer or release-policy changes.
- No BricsCAD V25/native/UI/runtime work.
- No GitHub Actions dispatch.

## Validation boundary

Source review plus focused Core smoke regression source only unless executable evidence is actually available. This remote session does not claim BricsCAD V25 runtime qualification.
