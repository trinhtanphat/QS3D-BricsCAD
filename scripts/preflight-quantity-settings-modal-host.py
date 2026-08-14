#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src" / "QS3D.BricsCAD.V25" / "QuantitySettingsCommands.cs").read_text(encoding="utf-8")
checks = {
    "QS3DSETUP registration": '[CommandMethod("QS3DSETUP", CommandFlags.Modal)]',
    "settings window": 'new QuantitySettingsWindow(new QuantitySettingsStore())',
    "BricsCAD modal host": 'Application.ShowModalWindow(window);',
    "contained diagnostics": 'WriteFailure(document, "QS3DSETUP", ex);',
    "nested diagnostics": 'current.InnerException',
    "long-name alias": '[CommandMethod("QS3DQUANTITYSETTINGS", CommandFlags.Modal)]',
}
missing = [name for name, token in checks.items() if token not in source]
if '.ShowDialog();' in source:
    missing.append("raw WPF ShowDialog is still used")
if missing:
    raise SystemExit("QS3DSETUP modal-host preflight failed: " + "; ".join(missing))
print("QS3DSETUP modal-host preflight passed.")
