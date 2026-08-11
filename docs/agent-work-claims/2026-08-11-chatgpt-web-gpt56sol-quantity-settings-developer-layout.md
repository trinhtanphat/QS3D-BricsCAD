# Work claim — Quantity Settings developer tab layout parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-developer-layout`
- Registered: `2026-08-11T21:35:00+07:00`
- Baseline main SHA: `650300db165f14f70fae688678cd4838bf57c5d7`
- Priority: P2 — screenshot-parity/readability continuation for the already-functional Setup & Rules developer thresholds.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml` developer tab only.
- `scripts/preflight-quantity-settings-developer-layout.py` new focused static gate.
- this claim file for close-out.

## Contract

- Reorganize the existing developer fields into the screenshot-inspired semantic groups without changing any x:Name, save/load binding or numeric meaning: general engine thresholds, quantity filtering, engulf/contained subtraction, Room pick, and quantity-explanation dimension labels.
- Add a warning/info banner making clear these are advanced thresholds.
- Keep all eleven existing input controls and their existing code-behind persistence unchanged.
- Add a live visual swatch bound to the existing `DimColorBox.Text`; do not add dependencies or a fake color-picker action.
- Preserve the completed three-pane Intersection Rules browser and Formwork tab verbatim in behavior.

## Exclusions

- No code-behind, `QuantitySettingsStore.cs`, Core settings/rule resolver/arithmetic, shared Theme, Ribbon, Workspace/RightPanel, updater/release, Direct Draw or GitHub Actions changes.
- No inference that the screenshot's threshold values are validated production engineering defaults beyond the existing persisted defaults.

## Validation plan

- Re-fetch current XAML before write; preserve concurrent winners.
- Add auto-discovered preflight that requires all eleven existing x:Names exactly once, the five section headings, live color swatch binding, and retained Intersection Rules browser tokens.
- Re-fetch final XAML and current main; no Actions dispatch and no remote native rendering PASS.

## Completion condition

- Developer settings visually match the supplied grouped workflow more closely while retaining the exact existing persistence/control contract, focused source gate is present, and this claim is closed with exact SHAs.
