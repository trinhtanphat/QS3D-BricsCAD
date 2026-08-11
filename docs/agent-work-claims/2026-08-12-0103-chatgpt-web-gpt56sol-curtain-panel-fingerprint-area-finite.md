# Work claim — Curtain panel fingerprint finite derived area

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-fingerprint-area-finite`
- Registered: `2026-08-12T01:03:00+07:00`
- Baseline main SHA: `a3c794205790ed43cc5a2e1dc9144bdf667ff345`
- Priority: P1 generated-output freshness fingerprint integrity.

## Confirmed defect

`CurtainWallPanelFingerprint.Validate(...)` requires each panel piece X/Z/Width/Height to be finite, positive where appropriate, and checks finite right/top bounds. It does not validate the derived `WidthM * HeightM` area. Two individually finite dimensions can overflow their product to positive infinity while the piece still receives a valid generated-panel fingerprint.

`CurtainWallPanelPiece.AreaM2` exposes that product directly. A freshness fingerprint must not certify a panel piece whose derived physical area is non-finite.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallPanelFingerprintAreaFiniteSmoke.cs` (new focused auto-registered smoke)
- this claim file

## Intended contract

- Fingerprinting rejects any piece whose finite positive width and height produce a non-finite area.
- Existing finite pieces, ordering, canonical serialization, SHA-256 format and source/path validation remain unchanged.
- No changes to curtain layout/opening generation or BricsCAD native code.

## Validation plan

- Construct a piece with `WidthM = 1e308`, `HeightM = 1e308`, finite X/Z and prove `Compute(...)` fails closed with overflow.
- Confirm an ordinary finite piece still produces a 64-character SHA-256 fingerprint and remains deterministic.
- Re-fetch source before write, SHA-guard update, inspect exact commits, then close claim.
- No GitHub Actions dispatch; no .NET/BricsCAD runtime PASS claim from this hosted environment.

## Completion condition

Non-finite derived panel area cannot be fingerprinted as valid generated state, regression source is on `main`, and this claim is closed.