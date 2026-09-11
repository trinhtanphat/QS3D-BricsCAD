#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/EstimatingRateWorkspaceCommands.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/EstimatingRateWorkspaceWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/EstimatingRateWorkspaceWindow.xaml.cs"
THEME = ROOT / "src/QS3D.BricsCAD.V25/UI/EstimatingRateWorkspaceWindow.DarkHostTheme.cs"
NAV = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.EstimateWorkspace.cs"
errors = []

def load(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing required estimating workspace surface: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

def require(source: str, token: str, label: str):
    if token not in source:
        errors.append(f"{label}: missing {token!r}")

command = load(COMMAND)
xaml = load(XAML)
code = load(CODE)
theme = load(THEME)
nav = load(NAV)

for token in ('[CommandMethod("QS3DESTIMATING", CommandFlags.Modal)]',
              "Application.ShowModelessWindow", "HasExactAffinity(document, identity)",
              "NativeDatabaseIdentity(document)"):
    require(command, token, "command")
for token in ("BILL ITEMS", "BUILD-UPS", "RATE REFERENCES", "BQ LIBRARY", "TRADE / CFA",
              'Click="OnRefresh"', 'Click="OnPreviewAdjustment"', 'Click="OnApplyAdjustment"'):
    require(xaml, token, "window")

for token in (
    "ExistingProjectMutationContext.Require(_document, operation)",
    "ProjectTbqWorkspace.Open(project)",
    "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    "state.PreviewAdjustment()",
    "state.AnalyzeBuildUps(false)",
    "state.AnalyzeTrades()",
    "context.Workspace.PreviewAdjustment(adjustment, markup)",
    "context.Workspace.ApplyAdjustment(adjustment, markup)",
    "ProjectStateSnapshot.Capture(context.Project)",
    "ProjectContextCoordinator.Save(_document)",
    "DocumentBoundWindowLifetime.Attach(this, document)",
    "_document.Database.UnmanagedObject != _nativeDatabaseIdentity",
):
    require(code, token, "code-behind")

for token in ("OnSourceInitialized", "BgSelectedBrush", "SystemColors.HighlightBrushKey",
              "SystemColors.InactiveSelectionHighlightBrushKey"):
    require(theme, token, "dark host theme")

require(nav, '"QS3DESTIMATING"', "quantity estimating navigation")

for forbidden in (
    "AdjustedTotal =",
    "BaseTotal =",
    "TotalCost =",
    "CostPerCfaM2 =",
    "File.WriteAllText(",
):
    if forbidden in code:
        errors.append(f"code-behind: adapter-side estimating arithmetic/output is forbidden: {forbidden!r}")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS estimating & rate build-up workspace source guard")
