# Work claim — license XML root namespace guard

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:55:00+07:00`
- Baseline main SHA observed before reservation: `04ef0469d48b2ab866f17dca13e5ab72a3abf22a`
- Priority: continue-all remote-safe licensing parser integrity

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Defect to fix

`LicenseVerifier.Load(...)` currently validates the XML root by `XName.LocalName`. A namespaced root can therefore satisfy the `<qs3dLicense>` root-name check even though the supported persisted license schema is intentionally unnamespaced. The loader must fail closed when the root carries a default or prefixed namespace while preserving canonical unnamespaced licenses.

## Planned regression

- Keep the canonical unnamespaced signed-license path unchanged.
- Reject a default-namespaced `<qs3dLicense>` root.
- Reject a prefixed namespaced `qs3dLicense` root even when its child elements are deliberately unnamespaced.
- Preserve the existing wrong-root/DTD/signature/canonicalization behavior.

## Explicit exclusions

- No Project Browser/Start Center/browser XML work.
- No feature canonicalization or token-whitespace work already completed by earlier claims.
- No hardware fingerprint, entitlement, SKU, trial/subscription, signing-key, updater, installer or release policy changes.
- No BricsCAD V25/native/UI/runtime work.
- No GitHub Actions dispatch.

## Validation boundary

Source and focused smoke coverage will be reviewed through the GitHub connector. This remote session does not claim BricsCAD V25 runtime qualification; executable smoke/build evidence will only be reported if actually run.
