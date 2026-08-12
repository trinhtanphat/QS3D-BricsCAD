# Work claim — License verifier product input canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:13:00+07:00`
- Baseline main SHA: `7c05929fee1f7f3b90bece29e27debfed0f9f189`
- Priority: P1 Core licensing API integrity during owner-requested `continue all`
- Task Key: `CORE-LICENSE-VERIFIER-PRODUCT-INPUT-CANONICALITY`

## Confirmed defect

`LicenseDocument.ProductId` is validated as a canonical signed token: nonblank, no leading/trailing whitespace, bounded to 128 UTF-16 code units, no control characters, and well-formed under the strict UTF-8 encoder. `LicenseVerifier.Verify(...)`, however, only rejects a blank `expectedProductId`. A malformed verifier/configuration input such as `" QS3D "` is therefore classified as an authentic `ProductMismatch` rather than rejected at the API boundary, conflating caller/configuration corruption with a valid signed-license mismatch.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- focused Core smoke coverage for verifier expected-product input canonicality
- this claim file for close-out

## Contract

- reject a noncanonical `expectedProductId` before verification result classification;
- reuse the exact ProductId token contract already enforced by `LicenseDocument` rather than introducing a second spelling/canonicalization rule;
- preserve canonical product match/mismatch behavior, signature verification, validity-window behavior, signed payload bytes, XML loading, key handling, features and UI/native BricsCAD behavior;
- do not silently trim or normalize verifier input.

## Validation plan

Add deterministic auto-registered Core smoke coverage proving padded/control/overlength/malformed-Unicode expected product ids fail closed while a canonical different product still returns `ProductMismatch` and a canonical matching signed product remains valid. Re-fetch current source/claim before each write and inspect exact pushed diffs. No force-push, GitHub Actions dispatch, executable .NET smoke/build PASS, Python PASS or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.