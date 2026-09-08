#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CommercialQsCommands.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/CommercialQsWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/CommercialQsWindow.xaml.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

errors = []

def require_file(path: Path):
    if not path.is_file():
        errors.append(f"missing required Commercial QS surface: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

def require(source: str, token: str, label: str):
    if token not in source:
        errors.append(f"{label}: missing {token!r}")

command = require_file(COMMAND)
xaml = require_file(XAML)
code = require_file(CODE)
registration = require_file(REGISTRATION)

require(command, '[CommandMethod("QS3DCOMMERCIAL", CommandFlags.Modal)]', "command")
require(command, "Application.ShowModelessWindow", "command")
require(command, "new CommercialQsWindow(document)", "command")

for token in ("VARIATION", "IPC", "FINAL ACCOUNT", "EXPORT"):
    require(xaml, token, "window")
require(xaml, 'SelectionChanged="OnVariationSelectionChanged"', "window")
require(xaml, 'Click="OnBuildVariationRegister"', "window")
require(xaml, 'Click="OnCreateIpc"', "window")
require(xaml, 'Click="OnReconcileFinalAccount"', "window")
require(xaml, 'Click="OnExportCommercialCsv"', "window")

for token in (
    "new CommercialVariationRegister(",
    "new ProgressClaimService().Evaluate(",
    "new InterimPaymentCertificateService().Create(",
    "new FinalAccountService().Reconcile(",
    "DocumentBoundWindowLifetime.Attach(this, document)",
    "WriteCommercialCsv(",
):
    require(code, token, "code-behind")

# The existing commercial Core smoke was previously source-only. Registration here makes
# the same domain authority exercised whenever the regular Core smoke suite runs.
require(registration, "CommercialQsSettlementSmoke.Run();", "smoke registration")

# Guard against adapter-side settlement arithmetic. UI may parse/format values, but the
# monetary authorities above must own gross/net/final reconciliation.
for forbidden in (
    "GrossCertifiedThisPeriod =",
    "NetCertifiedThisPeriod =",
    "FinalContractValue =",
    "AmountDue =",
    "RecoveryDue =",
):
    if forbidden in code:
        errors.append(f"code-behind: adapter-side commercial arithmetic assignment is forbidden: {forbidden!r}")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS commercial QS workspace source guard")
