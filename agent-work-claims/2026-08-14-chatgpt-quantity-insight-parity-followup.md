# Work claim — Quantity Insight BLT parity follow-up

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-insight-followup-20260814`
- Registered: `2026-08-14T16:13:00+07:00`
- Baseline main SHA: `5124e588275ac33f01c722218c9466ce76f03d12`
- Context: follow-up to the completed Quantity Insight BLT parity/click-to-3D lane after user requested `continue all`.

## Claimed scope

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs` for stale/dirty detail consistency when locating exact BREP deduction rows.
- One focused source regression/preflight guard for Quantity Insight BLT-parity wiring if an existing guard does not already cover it.
- This claim document for closeout evidence.

## Explicit exclusions

- No changes to Core quantity arithmetic, intersection/deduction rules, quantity mapping/category policy, or BREP computation contracts.
- No changes to Quantity Summary, Wall Quantity, Project Browser/Grid work, releases/signing, or GitHub Actions policy.
- No native-runtime PASS claim without a licensed BricsCAD V25 host.

## Proven gap and plan

The displayed component detail is generated from a detached preview that regenerates dirty semantic state, while the BREP exact explainer can also derive from live Solid3d. The current deduction-click path re-queries `ProjectQuantityReportBuilder.Detail` directly against the potentially dirty live semantic project before resolving CAD handles. This can make the visible exact deduction row fresh while its click-to-3D validation is stale.

1. Re-read current `main` after this claim lands and verify no overlapping source claim appeared.
2. Make deduction-row validation use the same detached preview + regeneration semantics as the displayed detail, while resolving actual CAD handles only from the live project.
3. Reject stale row/provenance changes consistently instead of locating mismatched data.
4. Add/read back focused regression evidence, commit/push `main`, and mark this claim `COMPLETED` with exact SHA(s).
