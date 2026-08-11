# Work claim — Quantity Settings runtime rule resolution

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-runtime-rules`
- Registered: `2026-08-11T20:58:00+07:00`
- Baseline main SHA: `21515fcb529fbce6712e3ce4968a27d8f65430f6`
- Priority: P1 — continue the owner-requested Setup & Rules feature beyond UI/persistence with a safe Core-side effective-rule lookup contract.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationRuleSet.cs` (new)
- `scripts/preflight-quantity-calculation-rule-set.py` (new)
- this claim file for close-out

## Contract

- Build an immutable/defensive runtime snapshot over `QuantityCalculationSettings` with deterministic category and directed intersection lookup.
- Native QS3D category codes resolve exactly first.
- Legacy BLT compatibility fallback is limited to category-code/name equivalences already established in the existing Quantity Settings UI; do not invent mappings for ambiguous BLT categories such as wall ties, lintels or ramps.
- Directed intersection lookup must preserve source -> target direction and must never mirror, synthesize or mutate a missing pair.
- Unknown imported category codes remain valid for exact integer-code lookup.
- Invalid settings fail closed through the existing `NormalizeAndValidate()` contract.

## Excluded scope

- No edits to `QuantitySettingsWindow.xaml` / `.xaml.cs`; an ACTIVE intersection-browser agent owns those files.
- No edits to `QuantitySettingsStore.cs`; an ACTIVE local V25 build-fix agent owns it.
- No changes to formwork/intersection geometry arithmetic, `StructuralRegenerator`, `ProjectQuantityReportBuilder`, Ribbon, shared theme, Workspace/RightPanel, updater/release or GitHub Actions.
- No claim that BLT intersection subtraction semantics can be executed without the required CAD contact/intersection geometry pipeline.

## Validation plan

- Add an auto-discovered static preflight proving defensive cloning, exact-native preference, compatibility fallback only for established mappings, directed pair semantics, unknown-code exact lookup and no synthetic missing rule.
- Re-fetch current `main` before implementation and preserve concurrent winners.
- Do not dispatch GitHub Actions.

## Coordination

- This lane intentionally avoids the ACTIVE Quantity Settings UI browser and V25 settings-store claims.
- Native BricsCAD contact/intersection geometry remains LOCAL_ONLY and is not represented as remote PASS evidence.

## Completion condition

- Core consumers have a deterministic effective-rule resolver that can safely consume native or imported BLT-compatible payloads without category-code casts or inferred engineering semantics, the focused source preflight is present, and this claim is marked `COMPLETED` with implementation evidence.
