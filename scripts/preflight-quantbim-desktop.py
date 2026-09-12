#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
project = root / "src" / "QS3D.QuantBim.Desktop"
required = [
    project / "QS3D.QuantBim.Desktop.csproj",
    project / "Program.cs",
    project / "QuantBimMainForm.cs",
    project / "SoftwareViewport.cs",
    project / "DesktopSelfTest.cs",
]
missing = [str(p.relative_to(root)) for p in required if not p.exists()]
if missing:
    raise SystemExit("missing QuantBIM desktop files: " + ", ".join(missing))

all_text = "\n".join(p.read_text(encoding="utf-8") for p in required)
csproj = required[0].read_text(encoding="utf-8")
form = (project / "QuantBimMainForm.cs").read_text(encoding="utf-8")
viewport = (project / "SoftwareViewport.cs").read_text(encoding="utf-8")

checks = {
    "windows target": "<TargetFramework>net8.0-windows</TargetFramework>" in csproj,
    "WinForms enabled": "<UseWindowsForms>true</UseWindowsForms>" in csproj,
    "single-file packaging": "<PublishSingleFile>true</PublishSingleFile>" in csproj,
    "Core-only project reference": "QS3D.Core\\QS3D.Core.csproj" in csproj,
    "STEP source": "IfcStepStandaloneSource" in form,
    "standalone workbench": "QuantBimStandaloneWorkbench" in form,
    "viewport host": "QuantBimStandaloneViewportHost" in form,
    "property inspection": "Inspect(" in form,
    "spatial/type filter": "IfcWorkbenchFilter" in form and "_storey" in form and "_type" in form,
    "selection QTO": "BuildSelectionBoq" in form,
    "CSV export": "ExportSelectionBoqCsv" in form,
    "mesh renderer": "IQuantBimViewportRenderer" in viewport and "TriangleIndices" in viewport,
    "navigation": all(token in viewport for token in ["FitAll", "Orbit", "Pan", "Zoom"]),
    "standard views": "QuantBimViewportStandardView" in viewport,
    "self test": "--self-test" in all_text and "DesktopSelfTest.Run" in all_text,
}
failed = [name for name, ok in checks.items() if not ok]
if failed:
    raise SystemExit("QuantBIM desktop contract failed: " + ", ".join(failed))

for forbidden in ("Bricscad", "Teigha", "BricsCAD.V25", "BricsCAD.V26", "ApplicationServices.Document"):
    if forbidden.lower() in all_text.lower():
        raise SystemExit("QuantBIM standalone desktop contains forbidden CAD-host dependency: " + forbidden)

print("QuantBIM standalone desktop preflight PASS")
