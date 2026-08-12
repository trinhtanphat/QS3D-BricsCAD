# Work claim — License XML attribute schema integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-attribute-schema-20260812-0837`
- Registered: `2026-08-12T08:37:00+07:00`
- Baseline main SHA: `e3e2bc1cbe1828f7c8f4a70718c16c9a803f9c1e`
- Priority: P1 — signed offline license XML must fail closed on unsigned structural metadata.

## Confirmed defect

`LicenseVerifier.Load(...)` validates recognized child element names/cardinality but does not validate attribute allowlists on the root, `valid`, `features`, `feature`, or `signature` elements. Unknown or namespaced attributes can therefore be added to a license without being represented in `CanonicalPayload()`, and the loader silently ignores them while the signature remains bound only to canonical fields.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- one isolated Core smoke file for license attribute-schema integrity
- this claim file for close-out

## Contract

- Root attributes are limited to `schema`, `id`, `customer`, `product`, `nonce`.
- `valid` attributes are limited to `notBeforeUtc`, `expiresUtc`.
- `features` accepts no attributes; each `feature` accepts only `name`; `signature` accepts only `algorithm`.
- Reject namespace declarations and namespaced/unknown attributes on these license grammar elements.
- Preserve existing child-shape/cardinality, payload/signature, time/product, token, DTD and file-size behavior.
- Do not modify `LicenseVerifierSmoke.cs`; use isolated module-initializer coverage.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
