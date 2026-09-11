#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Review/BqReviewLedger.cs"
STORE = ROOT / "src/QS3D.Core/Domain/ProjectBqReviewLedger.cs"
CODEC = ROOT / "src/QS3D.Core/Domain/ProjectBqReviewLedgerCodec.cs"
META = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
UI = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.BqReview.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BqReviewLedgerSmoke.cs"
errors = []

def load(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing BOQ review ledger surface: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

def require(source: str, token: str, label: str):
    if token not in source:
        errors.append(f"{label}: missing {token!r}")

core = load(CORE)
store = load(STORE)
codec = load(CODEC)
meta = load(META)
ui = load(UI)
xaml = load(XAML)
smoke = load(SMOKE)

for token in (
    "public enum BqReviewStatus",
    "public enum BqReviewMetric",
    "SourceSignature(QuantityReportRow row)",
    "row.DrawingFingerprint",
    "row.SourceHandles",
    "EffectiveApprovedValue",
    "Non-pending BQ review requires a reviewer.",
    "cannot make the proposed quantity negative",
):
    require(core, token, "core review contract")

for token in (
    "ProjectBqReviewLedgerCodec.Read(_metadata)",
    "_metadata.SetOwned(ProjectBqReviewLedgerCodec.LedgerKey, value)",
    "_metadata.RemoveOwned(ProjectBqReviewLedgerCodec.LedgerKey)",
):
    require(store, token, "project ledger store")

for token in (
    'ReservedRoot = "QS3D.BQReview."',
    'LedgerKey = Prefix + "Ledger"',
    "MaxPayloadChars = 1024 * 1024",
    "PersistedTextXml.Verify",
    "if (offset != payload.Length)",
):
    require(codec, token, "ledger codec")

for token in (
    "ProjectBqReviewLedgerCodec.IsReservedKey(key)",
    "ProjectBqReviewLedgerCodec.Read(metadata)",
):
    require(meta, token, "project metadata reservation")

for token in (
    "ExistingProjectMutationContext.Require(_document, \"lưu review BQ\")",
    "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    "ProjectStateSnapshot.Capture(project)",
    "ProjectContextCoordinator.Save(_document)",
    "AuditTrail.ForProject(project).Record",
    "FindFreshDetailRow(elementId)",
    "BqReviewService.CreateEntry",
):
    require(ui, token, "quantity review UI")

for token in (
    'x:Name="BqReviewPanel"',
    'Loaded="OnBqReviewPanelLoaded"',
    'SelectionChanged="OnBqReviewMetricChanged"',
    'Click="OnSaveBqReviewClick"',
    'Click="OnClearBqReviewClick"',
):
    require(xaml, token, "quantity review XAML")

for token in (
    "[ModuleInitializer]",
    "drawing fingerprint change must stale approval",
    "source provenance change must stale approval",
    "roundtrip adjustment delta",
    "snapshot must restore ledger",
):
    require(smoke, token, "BOQ review smoke")

for forbidden in (
    "row.GrossConcreteM3 =",
    "row.NetConcreteM3 =",
    "row.FormworkM2 =",
    "row.LengthM =",
):
    if forbidden in ui:
        errors.append(f"quantity review UI must not rewrite authoritative measured quantity: {forbidden!r}")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS BOQ review approval/manual-adjustment ledger source guard")
