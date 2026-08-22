#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []


def read(relative):
    path = ADAPTER / relative
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


lifecycle = read("DocumentLifecycleCoordinator.cs")
palette = read("PaletteCoordinator.cs")

require(lifecycle, "docs.DocumentDestroyed += OnDocumentDestroyed;", "lifecycle start")
require(lifecycle, "docs.DocumentDestroyed -= OnDocumentDestroyed;", "lifecycle stop")
require(lifecycle, "private static void OnDocumentDestroyed(object sender, DocumentDestroyedEventArgs e)", "destroyed handler")
require(lifecycle, "if (docs.Count == 0)", "last-document guard")
require(lifecycle, "PaletteCoordinator.ResetForNoDocument();", "last-document palette reset")
require(lifecycle, "EnsureProject(active, true);", "remaining-document rebind")
require(palette, "public static void ResetForNoDocument()", "palette reset API")
require(palette, "var workspaceVisible = IsWorkspaceVisible;", "workspace visibility preservation")
require(palette, "var rightVisible = IsRightPanelVisible;", "right visibility preservation")
require(palette, "Dispose();", "stale palette teardown")
require(palette, "EnsureCreated();", "empty palette recreation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: destroyed documents cannot leave stale workspace callbacks; remaining drawings rebind and the no-document palette preserves visibility.")
