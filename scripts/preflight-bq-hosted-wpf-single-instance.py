#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.SingleInstance.cs"
PROJECT = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"

source = SOURCE.read_text(encoding="utf-8")
project = PROJECT.read_text(encoding="utf-8")

required_source = [
    "PresentationSource.CurrentSources",
    "EnumerateLiveReviewWindows()",
    "HashSet<QuantitySummaryWindow>",
    "System.Windows.Application.Current",
    "ReferenceEquals(window._document, _document)",
    "existing.RefreshRowsForCurrentMode(false)",
    "existing.Activate()",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"Hosted-WPF BQ single-instance contract missing: {token}")

if "if (application == null) return;" in source:
    raise SystemExit("Hosted-WPF BQ reuse must not depend on Application.Current being non-null.")

for forbidden in ("Dictionary<string, QuantitySummaryWindow>", "ConditionalWeakTable<", "WorkspaceFloatingToolHost"):
    if forbidden in source:
        raise SystemExit(f"BQ single-instance follow-up must not introduce a feature-specific host/registry: {forbidden}")

if "<OutputType>Library</OutputType>" not in project:
    raise SystemExit("V25 product boundary changed: expected hosted plugin OutputType=Library.")

print("BQ hosted-WPF single-instance source guard: PASS")
