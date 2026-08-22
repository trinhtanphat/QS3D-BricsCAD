#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing QuantitySummaryWindow.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "private void EnsureCurrentProject(string operation)",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out _)",
        "EnsureCurrentProject(\"tính lại BQ\")",
        "EnsureCurrentProject(\"định vị BQ\")",
        "EnsureCurrentProject(\"xuất BQ XLSX\")",
    ):
        if token not in text:
            errors.append("QuantitySummaryWindow.xaml.cs missing modeless project-safety token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("BQ modeless window must not create/cache replacement project state")
    if "ProjectStateSnapshot.Capture(project)" not in text:
        errors.append("BQ column preference mutation must retain snapshot rollback")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] BQ modeless callbacks remain DWG-bound, existing-project-only, and preference mutations retain rollback")
