# Work claim — Quantity Settings health export

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-health-export`
- Registered: `2026-08-11T22:24:00+07:00`
- Baseline main SHA observed: `254e97aa0535d2a1cf85a1a979821f03d63d7f42`
- Priority: P1 — make the completed matrix diagnostics portable for local qualification/support without exporting the machine settings path or mutating project/drawing/settings state.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationMatrixDiagnosticSnapshot.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticSnapshotSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/QuantityCalculationMatrixDiagnosticSnapshotSmokeRegistration.cs` (new)
- `src/QS3D.BricsCAD.V25/QuantitySettingsDiagnosticExportCommands.cs` (new)
- `scripts/preflight-quantity-settings-diagnostics-export.py` (new)
- this claim file for close-out

## Contract

- Build an immutable portable snapshot from validated Quantity Settings via `QuantityCalculationMatrixDiagnostics.Analyze(...)`.
- Snapshot contains schema version, observed codes, intersection-only codes, unreferenced category-rule codes, existing/expected directed-rule counts, completeness and every missing directed pair.
- Unknown integer category codes must remain exact; A -> B and B -> A remain distinct.
- Snapshot must contain no machine settings path, project/drawing identity, user identity, timestamps, CAD handles or inferred category mappings.
- Add modal command `QS3DQSETTINGSHEALTHEXPORT` that loads through `QuantitySettingsStore.Load()`, builds the snapshot, asks for a destination JSON path, and serializes only that snapshot.
- Export command may write only the user-selected diagnostics file. It must not call settings Save/Export/Import, project lifecycle APIs, CAD transactions or drawing mutation.

## Excluded scope

- No edits to existing Quantity Settings WPF/store/core settings files, report builders, CAD geometry, Ribbon/updater/release or GitHub Actions.
- No native-runtime PASS claim from this remote session.

## Validation plan

- Core smoke covers exact deterministic snapshot content, directed missing pairs, unknown codes and defensive caller non-mutation.
- Focused source preflight guards Load -> snapshot -> save-dialog -> JSON-write ordering and forbids settings/project/drawing writes or sensitive identity fields.
- Re-fetch final files from latest `main` and preserve concurrent winners.

## Completion condition

- Local/support workflows can export one sanitized JSON integrity report from BricsCAD without exposing the settings location or changing QS3D/drawing state.
