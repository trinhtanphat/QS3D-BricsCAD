# Work claim — license loader token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-loader-token-canonicality-20260812-0041`
- Registered: `2026-08-12T00:41:00+07:00`
- Baseline main SHA: `c910f6c1c61c0ddc8cb5c5e81adb35c4be7956c1`
- Priority: P1 — preserve signed license canonical-token validation at the XML ingestion boundary.

## Reserved scope

Close the loader/validator mismatch in `LicenseVerifier`: `LicenseDocument.Validate()` rejects leading/trailing whitespace on signed scalar/feature tokens, but the XML `Required(...)` helper currently trims attribute values before `Validate()` sees them. A non-canonical on-disk license attribute can therefore be silently normalized instead of rejected.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Intended fix

- Preserve raw required XML attribute text instead of trimming it in the generic `Required(...)` helper.
- Let existing exact schema/algorithm comparisons, exact timestamp parsing, and `LicenseDocument.Validate()` reject non-canonical surrounding whitespace rather than silently repairing it.
- Keep signature element Base64 whitespace handling unchanged; this claim is only about signed canonical attribute/token fields.
- Extend the existing licensing smoke to write temporary XML licenses containing padded `id` and padded feature `name` attributes and require `Load(...)` to fail closed.

## Explicit exclusions

- Signature algorithm/key policy changes.
- Product/time-window behavior.
- License file size/DTD/XML resolver controls.
- BricsCAD/runtime/UI/installer/updater changes.
- GitHub Actions dispatch or workflow edits.

## Coordination

The prior completed `license token canonical whitespace` lane hardened `LicenseDocument.Validate()` directly but did not change XML loader trimming. This claim is a narrow follow-up on the loader boundary and does not reopen the completed direct-document validation behavior.

## Validation boundary

Deterministic existing Core smoke coverage plus source/diff review. No GitHub Actions dispatch; no licensed BricsCAD runtime PASS claimed.

## Completion condition

The on-disk XML loader can no longer trim away prohibited token whitespace, the focused smoke is committed to current `main`, no neighboring ACTIVE claim is overwritten, and this claim records exact integration evidence.
