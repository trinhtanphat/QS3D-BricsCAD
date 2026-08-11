# Work claim — license token canonical whitespace

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-token-canonical-whitespace`
- Registered: `2026-08-11T22:08:00+07:00`
- Baseline main SHA: `34b871b659c4e7ee87a5d0bc9076367d4ac1b6af`
- Priority: close a deterministic signed-license canonicalization defect without inventing commercial licensing policy.

## Confirmed defect

`LicenseDocument.ValidateToken(...)` currently accepts nonblank identifiers/features with leading or trailing whitespace, so an in-memory document can be signed with values such as `" LIC-001 "` or `" quantity "`. `LicenseVerifier.Load(...)`, however, canonicalizes XML attributes through `Required(...).Trim()` before verification. The same logical license therefore cannot round-trip through the supported file loader without changing the bytes covered by the signature.

The signed format should have one canonical token representation. Since persisted XML already trims attribute values, in-memory signing/verification must fail closed when a token is not already trimmed rather than signing a representation the loader cannot reproduce.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Intended contract

- `LicenseId`, `CustomerId`, `ProductId`, `Nonce` and every feature must already equal their trimmed form before canonical payload generation/verification.
- Existing valid tokens, feature delimiter rejection, signature verification and validity-window behavior remain unchanged.
- XML loading continues to trim attribute text before constructing the in-memory document, preserving compatibility for ordinary persisted licenses.

## Excluded scope

- No SKU/trial/subscription/seat/machine/offline-grace/backend/key-rotation policy.
- No production signing credentials, updater, installer or release changes.
- No BricsCAD V25/native/UI/runtime work.
- No shared smoke registry edit; `LicenseVerifierSmoke` is already registered.
- No GitHub Actions dispatch.

## Validation plan

- Add focused smoke coverage showing leading/trailing whitespace on scalar tokens and feature tokens is rejected before signing.
- Preserve a normal signed-license success case.
- Re-fetch the two reserved blobs immediately before writes and use their current SHAs so concurrent edits fail rather than overwrite.
- Review exact commit diff and verify the claim/implementation remain reachable from current `main` without force-push.

## Coordination

The prior license feature-delimiter claim is `RELEASED`; no current recent claim reserves these Licensing source/test surfaces. Current updater, reporting, diagnostics, documentation, workspace, recognition and native authoring claims are disjoint.

## Completion condition

Signed-license token canonicalization is whitespace-stable across in-memory and XML-loaded representations, focused regression source is merged, and this claim is closed with the exact implementation commit and truthful validation scope.
