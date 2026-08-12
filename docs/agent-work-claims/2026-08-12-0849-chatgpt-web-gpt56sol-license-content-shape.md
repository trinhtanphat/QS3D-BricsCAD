# Work claim — License XML content-shape integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-content-shape-20260812-0849`
- Registered: `2026-08-12T08:49:00+07:00`
- Baseline main SHA: `b9cb6ed68605c5f80d117f6f5ce73564f08d08f2`
- Priority: P1 — signed offline license grammar must fail closed on ignored unsigned XML content.

## Confirmed defect

`LicenseVerifier.Load(...)` validates direct child element names and attribute allowlists, but still ignores non-element XML nodes on the root and does not validate content nodes inside `<valid>` or individual `<feature>` elements. Persisted input can therefore carry arbitrary non-whitespace text, comments/processing instructions, or nested markup in those serializer-owned structural elements while the loaded `LicenseDocument` and `CanonicalPayload()` remain unchanged.

`<features>` also validates only child element names and ignores other node kinds. This is a lossy signed-format boundary: unsupported persisted XML can be accepted and silently discarded by the semantic loader.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- one isolated Core smoke file for root/valid/features/feature content-shape integrity
- this claim file for close-out

## Contract

- Root and `<features>` may contain only whitespace text plus their already-allowed direct child elements.
- `<valid>` and each `<feature>` must be structurally empty apart from whitespace text.
- Reject non-whitespace text, comments, processing instructions, CDATA, or nested elements where the grammar does not define them.
- Preserve existing signature-element behavior owned by the completed signature-content lane; do not change Base64 whitespace policy here.
- Preserve existing child/cardinality, attribute-schema, canonical timestamp, token, signature/product/time, DTD and file-size behavior.
- Do not modify `LicenseVerifierSmoke.cs`; use isolated module-initializer coverage.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
