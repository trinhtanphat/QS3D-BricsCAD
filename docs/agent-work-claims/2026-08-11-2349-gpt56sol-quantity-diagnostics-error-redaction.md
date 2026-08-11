# Work claim — Quantity Settings diagnostics error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-diagnostics-error-redaction-20260811-2349`
- Registered: `2026-08-11T23:49:00+07:00`
- Baseline main SHA observed: `8a31100a74da2686dac3e368d5e42719cb8ef273`
- Priority: P1 — close an indirect machine-path disclosure in the otherwise sanitized Quantity Settings health commands.

## Confirmed defect

`QuantitySettingsStore.ReadAndValidate(path)` intentionally wraps malformed settings failures with the concrete source path (`Cannot read quantity settings template '<path>': ...`). Both `QS3DQSETTINGSHEALTH` and `QS3DQSETTINGSHEALTHEXPORT` currently catch `System.Exception ex` and append `ex.Message` to the BricsCAD command line. Therefore malformed primary/backup settings, and file-system failures during health export, can disclose a full machine settings path or user-selected filesystem path even though the completed diagnostic claims explicitly promised path-safe output.

## Reserved scope

- `src/QS3D.BricsCAD.V25/QuantitySettingsDiagnosticCommands.cs`
- `src/QS3D.BricsCAD.V25/QuantitySettingsDiagnosticExportCommands.cs`
- `scripts/preflight-quantity-settings-diagnostics-path-redaction.py` (new)
- this claim file for close-out

## Contract

- Keep `QuantitySettingsStore` path-rich exceptions unchanged because the interactive Settings UI may legitimately use detailed local diagnostics.
- The two sanitized command-line diagnostics must not echo exception messages, stack traces, exception `ToString()`, settings paths, or selected output paths on failure.
- Emit stable generic command-specific failure text only.
- Preserve all success output, read-only settings/project/drawing boundaries, bounded detail output, Load -> Analyze/Snapshot ordering and user-selected export behavior.
- Do not weaken unsupported-future-schema fail-closed behavior.

## Excluded scope

- No edits to `QuantitySettingsStore.cs`, Quantity Settings WPF, Core rule/matrix/deduction models, project persistence, CAD geometry, Ribbon/Start Center, updater/release or GitHub Actions.
- No exception swallowing outside these two command surfaces.

## Validation plan

- Focused static preflight proves the store still contains a path-bearing detailed exception (the threat source), while both commands contain no `ex.Message`, `.ToString()`, `StackTrace`, `SettingsPath`, or full-path output in catch paths.
- Pin generic failure literals and retain success-path `Path.GetFileName(...)` only for the export result.
- Re-fetch latest main before implementation/merge and preserve concurrent winners without force push.
- No GitHub Actions dispatch; native runtime PASS is not claimed from this remote lane.

## Coordination

The original Quantity Settings health command/export claims are `COMPLETED`. Current active ProjectSession/opening/takeoff and other source lanes do not own these two diagnostic command files. This lane is intentionally narrow and non-overlapping.

## Completion condition

Malformed settings/export failures can no longer leak local filesystem paths through the two sanitized Quantity Settings diagnostics commands, focused source regression is merged to `main`, and this claim is marked `COMPLETED` with exact merge evidence.
