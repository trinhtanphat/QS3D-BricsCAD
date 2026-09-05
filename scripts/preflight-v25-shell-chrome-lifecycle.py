#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/Blt3dShellChromeCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

required = {
    "weak registry": "WeakReference<FrameworkElement>",
    "weak lookup": "TryGetTarget",
    "dead-entry pruning": "HiddenElements.RemoveAt",
    "live-only reset": "TryGetElement",
}
missing = [name for name, token in required.items() if token not in text]
if missing:
    raise SystemExit("FAIL shell chrome lifecycle guard: missing " + ", ".join(missing))

forbidden = {
    "strong FrameworkElement property": "public FrameworkElement Element { get; }",
    "strong constructor assignment": "Element = element;",
}
found = [name for name, token in forbidden.items() if token in text]
if found:
    raise SystemExit("FAIL shell chrome lifecycle guard: retained " + ", ".join(found))

if "ReferenceEquals(current, element)" not in text:
    raise SystemExit("FAIL shell chrome lifecycle guard: live duplicate suppression is missing")

print("PASS V25 shell chrome lifecycle uses weak live-only registry semantics")
