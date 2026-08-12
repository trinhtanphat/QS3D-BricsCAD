# Work claim — License XML attribute schema integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-attribute-schema-20260812-0837`
- Registered: `2026-08-12T08:37:00+07:00`
- Completed: `2026-08-12T08:42:00+07:00`
- Baseline main SHA: `e3e2bc1cbe1828f7c8f4a70718c16c9a803f9c1e`
- Priority: P1 — signed offline license XML must fail closed on unsigned structural metadata.
- Integration PR: `#655`
- Main integration commit: `4aae4eb775fcd2fec8b3c7e9f44e62f475bcc98f`

## Confirmed defect

`LicenseVerifier.Load(...)` validated recognized child element names/cardinality but did not validate attribute allowlists on the root, `valid`, `features`, `feature`, or `signature` elements. Unknown or namespaced attributes could therefore be added to a license without being represented in `CanonicalPayload()`, and the loader silently ignored them while the signature remained bound only to canonical fields.

## Implemented scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseXmlAttributeSchemaSmoke.cs`
- this claim file for close-out

## Completed contract

- Root attributes are limited to `schema`, `id`, `customer`, `product`, `nonce`.
- `valid` attributes are limited to `notBeforeUtc`, `expiresUtc`.
- `features` accepts no attributes; each `feature` accepts only `name`; `signature` accepts only `algorithm`.
- Namespace declarations and namespaced/unknown attributes on these license grammar elements fail closed.
- Existing child-shape/cardinality, payload/signature, time/product, token, DTD and file-size behavior remains unchanged.
- `LicenseVerifierSmoke.cs` was not modified; isolated module-initializer coverage pins the new grammar boundary.

## Validation evidence

- Claim registration: `7805ab5d978147ce01f083dc98f4393e9537af04`.
- Branch source commit: `b0b73f7b2c80c4f409d9207c31165d75ad64ca88`.
- Branch smoke commit: `c7f5587dfd64bd83c1bd898f4de383336c06ec08`.
- The branch was repeatedly synchronized with moving `main` without force-push; PR `#655` ultimately merged to `main` as `4aae4eb775fcd2fec8b3c7e9f44e62f475bcc98f`.
- Post-merge readback confirms `ValidateAttributes(...)` is applied to all reserved license grammar elements and the isolated smoke is present on `main`.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source was re-read, and exact integration SHA/evidence is recorded above.
