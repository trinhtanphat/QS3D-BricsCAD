# Work claim — License signature non-text XML node shape

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-signature-node-shape-20260812-0919`
- Registered: `2026-08-12T09:19:00+07:00`
- Completed: `2026-08-12T09:21:00+07:00`
- Baseline main SHA: `0fcb532b8659f2ac2f534cb491b6ec53f8ae4f6f`
- Claim commit: `557880c7e10a28a3456f9f34ddbbed84eee6243c`
- Source fix commit: `cb55b30fd16e4d613ac5a105badb99376a149884`
- Focused smoke commit: `649918c8589d15dc720164c74ff1acaf7a31edf0`
- Priority: P1 — signed-license XML must not silently reinterpret unsupported signature child nodes.
- Task Key: `CORE-LICENSE-SIGNATURE-NONTEXT-NODE-SHAPE`

## Confirmed defect

The completed nested-signature lane rejected child elements via `signatureElement.HasElements`, but XML comments, CDATA and processing instructions are not child elements. `LicenseVerifier.Load(...)` subsequently read `XElement.Value`, which can ignore or flatten those nodes into the apparent Base64 signature. This allowed structurally non-text signature XML to be interpreted as ordinary signature bytes even though the rest of the license grammar now rejects unsupported XML node shapes.

## Implemented contract

- Existing nested-element rejection remains first and unchanged.
- `ValidateSignatureTextNodes(...)` then requires every remaining child node to be ordinary `XText`, explicitly rejecting `XCData` and all other node types such as comments and processing instructions.
- Existing surrounding text whitespace trimming, Base64 decoding/size checks, RSA-SHA256 algorithm, token/timestamp/schema rules and cryptographic verification remain unchanged.
- Canonical payload construction, public-key verification, other license sections, UI/native BricsCAD and release code were not modified.

## Validation evidence

- Current `main` readback confirms signature node validation executes after the existing `HasElements` guard and before `.Value` / Base64 decoding.
- `LicenseSignatureNodeShapeSmoke` is auto-registered and preserves a normal text signature with surrounding whitespace while rejecting comment-split Base64, CDATA Base64 and processing-instruction-interleaved Base64.
- The earlier nested-element signature regression remains untouched and authoritative for element-child rejection.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: the license signature grammar now accepts ordinary text only and no longer silently reinterprets unsupported non-text XML nodes.
