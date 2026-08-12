# Work claim — License verifier product input canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:13:00+07:00`
- Completed: `2026-08-12T10:16:00+07:00`
- Baseline main SHA: `7c05929fee1f7f3b90bece29e27debfed0f9f189`
- Priority: P1 Core licensing API integrity during owner-requested `continue all`
- Task Key: `CORE-LICENSE-VERIFIER-PRODUCT-INPUT-CANONICALITY`

## Confirmed defect

`LicenseDocument.ProductId` was validated as a canonical signed token: nonblank, no leading/trailing whitespace, bounded to 128 UTF-16 code units, no control characters, and well-formed under the strict UTF-8 encoder. `LicenseVerifier.Verify(...)`, however, only rejected a blank `expectedProductId`. A malformed verifier/configuration input such as `" QS3D "` was therefore classified as an authentic `ProductMismatch` rather than rejected at the API boundary, conflating caller/configuration corruption with a valid signed-license mismatch.

## Delivered contract

- `LicenseDocument.Validate()` and `LicenseVerifier.Verify(...)` now share one internal `ValidateProductId(...)` path backed by the existing signed-token validator;
- noncanonical `expectedProductId` values fail as `ArgumentException` before verification result classification, without trimming or normalization;
- canonical product mismatch still returns `ProductMismatch`;
- canonical product match continues into the existing signature/validity verification path;
- signed payload construction, XML loading, RSA-SHA256 verification, validity-window handling, feature handling and UI/native BricsCAD behavior were not changed.

## Commits

- Claim: `00e637ca322c4db5e31b9264b7f7374a9cd8f424`
- Source fix: `39513f100710e992fc3b029b9bcad3b89b869525`
- Focused smoke coverage: `e68a8a2fbc1f3b63e308c80c4fa64e66de9022ac`

## Validation

Readback from `main` confirmed the shared `ValidateProductId(...)` path is present in both `LicenseDocument.Validate()` and `LicenseVerifier.Verify(...)`. The committed auto-registered smoke covers padded, control-character, overlength and malformed-Unicode verifier product ids, plus canonical mismatch and canonical-match continuation into signature classification. At `main` SHA `67a7ca73b0fff9c626bfeba7cebdc4c00a50455f`, ancestry comparison confirmed source commit `39513f100710e992fc3b029b9bcad3b89b869525` remains an ancestor and no later concurrent commit had modified `LicenseVerifier.cs`; the smoke file was also present on `main`.

The smoke source was committed and read back but not executed in this connector session. No force-push, GitHub Actions dispatch, executable .NET smoke/build PASS, Python PASS or licensed BricsCAD V25/V26 runtime qualification is claimed.