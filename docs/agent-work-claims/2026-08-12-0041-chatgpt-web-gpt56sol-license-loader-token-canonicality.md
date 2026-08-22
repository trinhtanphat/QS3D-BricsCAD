# Work claim — license loader token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-loader-token-canonicality-20260812-0041`
- Registered: `2026-08-12T00:41:00+07:00`
- Baseline main SHA: `c910f6c1c61c0ddc8cb5c5e81adb35c4be7956c1`
- Priority: P1 — preserve signed license canonical-token validation at the XML ingestion boundary.

## Reserved scope

Close the loader/validator mismatch in `LicenseVerifier`: `LicenseDocument.Validate()` rejects leading/trailing whitespace on signed scalar/feature tokens, but the XML `Required(...)` helper trimmed attribute values before `Validate()` saw them. A non-canonical on-disk license attribute could therefore be silently normalized instead of rejected.

## Reserved surfaces

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Implemented fix

- `Required(...)` now preserves required XML attribute text instead of trimming it.
- Existing exact schema/algorithm comparisons, exact timestamp parsing, and `LicenseDocument.Validate()` now see the original attribute text and reject prohibited surrounding whitespace.
- Signature element Base64 whitespace handling remains unchanged.
- Existing licensing smoke now writes temporary XML licenses containing a padded `id` and padded feature `name` and requires `Load(...)` to fail closed.

## Integration evidence

- Claim registration: `e521ef23018c916e3dc46b29b7438d1c3e316867`.
- Source branch commits: `35720c832f4e6ff251789813e66798cd83b2fa0d`, `5606a79c8c9fbe79f57d95ef5765a67831f5efd1`.
- PR: `#611`.
- Squash merge on current `main`: `464d084c3afb5070e7af53996f8dd171a400daef`.
- Before merge, comparison from the claim commit to current main showed no intervening modification of either reserved licensing file, so integration did not overwrite neighboring work.

## Explicit exclusions

- Signature algorithm/key policy changes.
- Product/time-window behavior.
- License file size/DTD/XML resolver controls.
- BricsCAD/runtime/UI/installer/updater changes.
- GitHub Actions dispatch or workflow edits.

## Validation boundary

Deterministic committed Core smoke coverage plus source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD runtime PASS is claimed.
