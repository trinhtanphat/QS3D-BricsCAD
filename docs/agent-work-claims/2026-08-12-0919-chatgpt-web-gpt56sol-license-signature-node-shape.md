# Work claim — License signature non-text XML node shape

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-signature-node-shape-20260812-0919`
- Registered: `2026-08-12T09:19:00+07:00`
- Baseline main SHA: `0fcb532b8659f2ac2f534cb491b6ec53f8ae4f6f`
- Priority: P1 — signed-license XML must not silently reinterpret unsupported signature child nodes.
- Task Key: `CORE-LICENSE-SIGNATURE-NONTEXT-NODE-SHAPE`

## Confirmed defect

The completed signature-content lane rejects nested child elements through `signatureElement.HasElements`, and the later general XML-content lane hardens root/valid/features/feature nodes. However `<signature>` still reads `XElement.Value` after only the child-element check. XML comments, CDATA and processing instructions are not child elements; `XElement.Value` can ignore or flatten them into the apparent Base64 payload. For example `AA<!--ignored-->==` is interpreted as `AA==`, and CDATA containing `AA==` is accepted. This leaves the signature grammar more permissive than the rest of the strict license XML format.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseSignatureNodeShapeSmoke.cs`
- this claim file

## Intended contract

- Preserve the existing nested-element rejection and text-only RSA-SHA256 signature parsing.
- After nested-element rejection, require every remaining signature child node to be ordinary `XText`; explicitly reject `XCData`, comments, processing instructions and other non-text XML nodes.
- Preserve existing surrounding text whitespace trimming, Base64 decoding/size checks, signature algorithm, token/timestamp/schema rules and cryptographic verification.
- Do not modify canonical payload construction, public-key verification, other license sections, UI/native BricsCAD or release code.

## Validation plan

Focused auto-registered Core smoke verifies a normal text signature remains loadable while comment-split Base64, CDATA Base64 and processing-instruction-interleaved Base64 are rejected. Existing nested-element regression remains untouched. Re-fetch current source/claim before writes. No force-push, Actions dispatch, executable smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
