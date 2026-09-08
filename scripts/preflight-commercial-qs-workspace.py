#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CommercialQsCommands.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/CommercialQsWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/CommercialQsWindow.xaml.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/CommercialQsWorkbook.cs"
AUDIT = ROOT / "src/QS3D.Core/Commercial/CommercialWorkflowAudit.cs"

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
exporter = require_file(EXPORTER)
audit = require_file(AUDIT)

require(command, '[CommandMethod("QS3DCOMMERCIAL", CommandFlags.Modal)]', "command")
require(command, "Application.ShowModelessWindow", "command")
require(command, "new CommercialQsWindow(document)", "command")
for token in ("Variation", "IPC", "Final Account", "Tender", "CVR", "XLSX"):
    require(command, token, "command status")

for token in ("VARIATION", "IPC", "FINAL ACCOUNT", "TENDER / PROCUREMENT", "CVR / FORECAST", "EXPORT XLSX"):
    require(xaml, token, "window")
for token in (
    'SelectionChanged="OnVariationSelectionChanged"',
    'Click="OnBuildVariationRegister"',
    'Click="OnCreateIpc"',
    'Click="OnReconcileFinalAccount"',
    'Click="OnEvaluateTender"',
    'Click="OnAwardTender"',
    'Click="OnEvaluateCvr"',
    'Click="OnFreezeCvr"',
    'Click="OnReopenCvr"',
    'Click="OnReviseCvrForecast"',
    'Click="OnExportCommercialWorkbook"',
):
    require(xaml, token, "window")

for token in (
    "new CommercialVariationRegister(",
    "new ProgressClaimService().Evaluate(",
    "new InterimPaymentCertificateService().Create(",
    "new FinalAccountService().Reconcile(",
    "new TenderProcurementService().Evaluate(",
    "new TenderProcurementService().Award(",
    "new CommercialCostControlService().Evaluate(",
    ".Freeze(",
    ".Reopen(",
    ".ReviseForecast(",
    "CommercialQsWorkbook.Export(",
    "CommercialWorkflowAudit.",
    "AuditTrail.ForProject(",
    "ProjectContextCoordinator.GetOrCreate(_document)",
    "DocumentBoundWindowLifetime.Attach(this, document)",
):
    require(code, token, "code-behind")

for token in (
    "public sealed class CommercialQsWorkbookSnapshot",
    "public static class CommercialQsWorkbook",
    'public const string SchemaVersion = "QS3D_COMMERCIAL_QS_V1"',
    '"META"',
    '"VARIATIONS"',
    '"IPC"',
    '"FINAL_ACCOUNT"',
    '"TENDER"',
    '"CVR"',
    "XlsxPackageValidator.Validate(",
    "AtomicFileCommit.ReplaceWithoutBackup(",
):
    require(exporter, token, "Core workbook")

for token in (
    "public static class CommercialWorkflowAudit",
    '"commercial.tender.evaluated"',
    '"commercial.tender.awarded"',
    '"commercial.cvr.evaluated"',
    '"commercial.cvr.frozen"',
    '"commercial.cvr.reopened"',
    '"commercial.cvr.forecast-revised"',
    '"commercial.report.exported"',
    "audit.Record(",
):
    require(audit, token, "Core audit")

# Guard against adapter-side commercial arithmetic or report construction. UI may parse
# and format values, but Core authorities own all monetary outputs and workbook rows.
for forbidden in (
    "GrossCertifiedThisPeriod =",
    "NetCertifiedThisPeriod =",
    "FinalContractValue =",
    "AmountDue =",
    "RecoveryDue =",
    "EvaluatedTotal =",
    "ForecastFinalCost =",
    "ForecastVariance =",
    "CvrMargin =",
    "WriteCommercialCsv(",
    "File.WriteAllText(",
    "new ZipArchive(",
):
    if forbidden in code:
        errors.append(f"code-behind: adapter-side commercial arithmetic/report generation is forbidden: {forbidden!r}")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS commercial QS workspace source guard")
