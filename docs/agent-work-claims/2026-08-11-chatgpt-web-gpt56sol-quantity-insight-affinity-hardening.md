# Work claim — Quantity Insight modeless document/row affinity hardening

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-affinity-hardening`
- Registered: `2026-08-11T20:56:00+07:00`
- Baseline main SHA: `12159590d88afd2127f49404d254184883e4f0b5`
- Priority: P1

## Reserved scope

- Audit and harden the newly added docked `QuantityInsightPanel` so modeless rows cannot locate/select CAD objects after the active DWG/project or live quantity row has changed.
- Preserve the existing read-only quantity tree, selection highlight, native Handle selection + `QS3DZOOMSELECTED`, and the completed `QS3DBQ` detail/reveal lane.
- Add deterministic source/preflight coverage for the document-affinity and stale-row fail-closed contract.

## Expected files

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/ViewModels/QuantityInsightViewModel.cs` only if stable live-row identity needs to be retained in the item model
- `scripts/preflight-quantity-insight-affinity.py`
- this claim file for close-out

## Excluded scope

- Quantity formulas, intersection/formwork arithmetic, persistence schema, `QuantitySettingsWindow*`, updater/release work, Ribbon/Start Center, and Core reporting behavior.
- No remote claim of native BricsCAD V25 mouse/viewport PASS.

## Validation plan

- Re-fetch current main before substantive writes and preserve concurrent winners.
- Require the selected item to belong to the same active DWG/project snapshot used to populate the panel.
- Rebuild current grouped rows read-only and require exactly one live semantic row with matching identity/value/provenance before native CAD selection.
- Add an auto-discovered static preflight guarding ordering and non-creating read-only behavior.

## Completion condition

- Quantity Insight locate refuses stale/cross-DWG rows and only selects/zooms a revalidated live current row.
- Source preflight is committed and this claim is marked `COMPLETED` with exact implementation SHA evidence.
