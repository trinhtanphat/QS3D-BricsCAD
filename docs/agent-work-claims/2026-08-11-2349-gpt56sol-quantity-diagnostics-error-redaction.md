# Work claim — Quantity Settings diagnostics error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-diagnostics-error-redaction-20260811-2349`
- Registered: `2026-08-11T23:49:00+07:00`
- Completed: `2026-08-11T23:54:00+07:00`
- Baseline main SHA observed: `8a31100a74da2686dac3e368d5e42719cb8ef273`
- Priority: P1 — close an indirect machine-path disclosure in the otherwise sanitized Quantity Settings health commands.

## Confirmed defect

`QuantitySettingsStore.ReadAndValidate(path)` intentionally wraps malformed settings failures with the concrete source path (`Cannot read quantity settings template '<path>': ...`). Before this batch, both `QS3DQSETTINGSHEALTH` and `QS3DQSETTINGSHEALTHEXPORT` caught `System.Exception ex` and appended `ex.Message` to the BricsCAD command line. Malformed primary/backup settings or file-system failures during health export could therefore disclose a full machine settings path or user-selected filesystem path, contradicting the sanitized diagnostics contract.

## Delivered scope

- `src/QS3D.BricsCAD.V25/QuantitySettingsDiagnosticCommands.cs`
- `src/QS3D.BricsCAD.V25/QuantitySettingsDiagnosticExportCommands.cs`
- `scripts/preflight-quantity-settings-diagnostics-path-redaction.py`
- this claim file

## Implemented contract

- `QuantitySettingsStore` remains unchanged and may keep path-rich exceptions for the interactive Settings UI/local troubleshooting surface.
- `QS3DQSETTINGSHEALTH` now catches without binding an exception variable and emits only stable generic failure text.
- `QS3DQSETTINGSHEALTHEXPORT` does the same, preventing both settings-source paths and selected output paths from being echoed through exception messages.
- Success behavior remains unchanged: the export command reports only `Path.GetFileName(dialog.FileName)`, never the full selected path.
- Load -> Analyze/Snapshot ordering, bounded health detail output, user-selected export behavior and read-only project/drawing/settings boundaries remain unchanged.

## Regression coverage

`scripts/preflight-quantity-settings-diagnostics-path-redaction.py`:

- asserts that `QuantitySettingsStore` still contains the path-bearing detailed exception, so the indirect threat source cannot silently disappear from the test premise;
- requires both command registrations and their Load -> Analyze/Snapshot/export calls;
- forbids `ex.Message`, `exception.Message`, stack traces, exception `ToString()`, `SettingsPath`, `Path.GetFullPath` and environment-folder disclosure in both sanitized command surfaces;
- requires basename-only export success output and rejects direct concatenation of `dialog.FileName`.

## Product integration

- Claim registration: `02e9e9a565105dd05366f25f6c577f0465bf1f08`.
- PR: `#539` — `fix(quantity): redact settings diagnostics failure paths`.
- Squash merge on `main`: `ba1b63c0223364812b98cae5b4a744f354c302ac`.
- `main` advanced concurrently after claim registration; the branch was refreshed with current `main` via a non-force merge commit before squash merge, preserving concurrent winners.

## Validation actually performed

- Re-fetched the two source commands before implementation and confirmed they directly appended `ex.Message`.
- Re-fetched `QuantitySettingsStore` and confirmed its malformed-template wrapper contains the concrete `path`, proving the disclosure path was real rather than hypothetical.
- PR `#539` became mergeable only after branch/current-main reconciliation and was squash-merged without force push.
- Source/static review only in this remote session; the focused preflight was not executed from a repository checkout, so no execution PASS is claimed.
- No GitHub Actions or release workflow was dispatched. No licensed BricsCAD V25 runtime PASS is claimed.

## Coordination

The original Quantity Settings health command/export claims are completed. No `QuantitySettingsStore`, WPF, Core rule/matrix/deduction, project persistence, CAD geometry, Ribbon/Start Center, updater or release files were modified.

## Completion

Reservation released. Sanitized Quantity Settings health failures no longer echo path-bearing exception details, while local detailed store exceptions and basename-only success output are preserved.
