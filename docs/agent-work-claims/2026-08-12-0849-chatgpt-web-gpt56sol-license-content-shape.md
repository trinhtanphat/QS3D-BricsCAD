# Work claim — License XML content-shape integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-content-shape-20260812-0849`
- Registered: `2026-08-12T08:49:00+07:00`
- Completed: `2026-08-12T08:53:00+07:00`
- Baseline main SHA: `b9cb6ed68605c5f80d117f6f5ce73564f08d08f2`
- Priority: P1 — signed offline license grammar must fail closed on ignored unsigned XML content.
- Integration PR: `#665`
- Main integration commit: `247575425227e8f21849b84f6fe7dcc0393b7099`

## Confirmed defect

`LicenseVerifier.Load(...)` validated direct child element names and attribute allowlists, but ignored non-element XML nodes on the root and did not validate content nodes inside `<valid>` or individual `<feature>` elements. Persisted input could therefore carry arbitrary non-whitespace text, comments/processing instructions, or nested markup in serializer-owned structural elements while the loaded `LicenseDocument` and `CanonicalPayload()` remained unchanged.

`<features>` also validated only child element names and ignored other node kinds. This was a lossy signed-format boundary: unsupported persisted XML could be accepted and silently discarded by the semantic loader.

## Implemented scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseXmlContentShapeSmoke.cs`
- this claim file for close-out

## Completed contract

- Root and `<features>` may contain only whitespace text plus their already-allowed direct child elements.
- `<valid>` and each `<feature>` must be structurally empty apart from whitespace text.
- Non-whitespace text, comments, processing instructions, CDATA, or nested elements where the grammar does not define them fail closed.
- Existing signature-element behavior from the completed signature-content lane is preserved; Base64 whitespace policy was not changed here.
- Existing child/cardinality, attribute-schema, canonical timestamp, token, signature/product/time, DTD and file-size behavior remains intact.

## Validation evidence

- Claim registration: `89cac50d41bb3c9efb796f79b94ac94338e05b2a`.
- Branch source commit: `fdb35b9fb4fcd8286a621112419ba6922a3a7b1c`.
- Branch smoke commit: `5adab9f27ee5ebfbb542ba579e612d025f913175`.
- Branch was synchronized with moving `main` without force-push and PR `#665` squash-merged to `main` as `247575425227e8f21849b84f6fe7dcc0393b7099`.
- Post-merge readback confirms `ValidateStructuredContent(...)` is applied to root, `valid`, `features`, and each `feature`, and isolated smoke coverage is present.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source was re-read, and exact integration SHA/evidence is recorded above.
