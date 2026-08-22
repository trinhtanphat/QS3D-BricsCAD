# Work claim — Curtain panel fingerprint finite derived area

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-fingerprint-area-finite`
- Registered: `2026-08-12T01:03:00+07:00`
- Completed: `2026-08-12T01:05:00+07:00`
- Baseline main SHA: `a3c794205790ed43cc5a2e1dc9144bdf667ff345`
- Priority: P1 generated-output freshness fingerprint integrity.

## Confirmed defect

`CurtainWallPanelFingerprint.Validate(...)` required each panel piece X/Z/Width/Height to be finite, positive where appropriate, and checked finite right/top bounds. It did not validate the derived `WidthM * HeightM` area. Two individually finite dimensions could overflow their product to positive infinity while the piece still received a valid generated-panel fingerprint.

`CurtainWallPanelPiece.AreaM2` exposes that product directly. A freshness fingerprint must not certify a panel piece whose derived physical area is non-finite.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallPanelFingerprintAreaFiniteSmoke.cs`
- this claim file

## Completed contract

- Fingerprinting now rejects any piece whose finite positive width and height produce a non-finite area.
- Existing finite pieces, ordering, canonical serialization, SHA-256 format and source/path validation remain unchanged.
- No changes were made to curtain layout/opening generation or BricsCAD native code.

## Published commits

- Source fix: `9a33338630393fb6d41da77654c127fde82e6802` — `fix(curtain): reject non-finite panel fingerprint area`
- Focused regression: `23b4ec0bec0dd7b75f165046dbd30c7cc53bb5e7` — `test(curtain): guard panel fingerprint area overflow`

## Validation notes

- Exact source diff was reviewed after publication: only the derived-area finite guard was added to `CurtainWallPanelFingerprint.Validate(...)`.
- The focused auto-registered smoke covers `1e308 * 1e308` overflow rejection and deterministic 64-character SHA-256 output for an ordinary finite piece.
- No GitHub Actions were dispatched.
- This hosted environment did not execute the .NET smoke binary or BricsCAD runtime, so no executable/runtime PASS is claimed.

## Completion condition

Satisfied: non-finite derived panel area cannot be fingerprinted as valid generated state, regression source is on `main`, and this claim is closed.