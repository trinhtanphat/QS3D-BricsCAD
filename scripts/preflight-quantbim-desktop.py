#!/usr/bin/env python3
from pathlib import Path
import os
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
project = root / "src" / "QS3D.QuantBim.Desktop"
project_file = project / "QS3D.QuantBim.Desktop.csproj"
required = [
    project_file,
    project / "Program.cs",
    project / "QuantBimMainForm.cs",
    project / "SoftwareViewport.cs",
    project / "DesktopSelfTest.cs",
]
missing = [str(p.relative_to(root)) for p in required if not p.exists()]
if missing:
    raise SystemExit("missing QuantBIM desktop files: " + ", ".join(missing))

all_text = "\n".join(p.read_text(encoding="utf-8") for p in required)
csproj = project_file.read_text(encoding="utf-8")
form = (project / "QuantBimMainForm.cs").read_text(encoding="utf-8")
viewport = (project / "SoftwareViewport.cs").read_text(encoding="utf-8")

checks = {
    "windows target": "<TargetFramework>net8.0-windows</TargetFramework>" in csproj,
    "WinForms enabled": "<UseWindowsForms>true</UseWindowsForms>" in csproj,
    "Windows targeting enabled": "<EnableWindowsTargeting>true</EnableWindowsTargeting>" in csproj,
    "single-file packaging": "<PublishSingleFile>true</PublishSingleFile>" in csproj,
    "framework-dependent packaging": "<SelfContained>false</SelfContained>" in csproj,
    "win-x64 packaging": "<RuntimeIdentifier>win-x64</RuntimeIdentifier>" in csproj,
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


def desktop_changed_in_ci():
    if os.environ.get("GITHUB_ACTIONS", "").lower() != "true":
        return False
    probe = subprocess.run(
        ["git", "diff", "--quiet", "origin/main...HEAD", "--", "src/QS3D.QuantBim.Desktop"],
        cwd=root,
        check=False,
    )
    if probe.returncode == 0:
        return False
    if probe.returncode == 1:
        return True
    raise SystemExit("could not determine QuantBIM desktop change scope from origin/main...HEAD")


def run(command):
    completed = subprocess.run(command, cwd=root, check=False, text=True)
    if completed.returncode != 0:
        raise SystemExit("QuantBIM desktop command failed (exit %d): %s" % (completed.returncode, " ".join(command)))


if desktop_changed_in_ci():
    run(["dotnet", "build", str(project_file), "-c", "Release"])
    run(["dotnet", "run", "--project", str(project_file), "-c", "Release", "--no-build", "--", "--self-test"])
    with tempfile.TemporaryDirectory(prefix="qs3d-quantbim-publish-") as publish_dir:
        run([
            "dotnet", "publish", str(project_file), "-c", "Release", "-r", "win-x64",
            "--no-self-contained", "--no-restore", "-o", publish_dir,
        ])
        published = Path(publish_dir) / "QS3D.QuantBim.Desktop.exe"
        if not published.is_file() or published.stat().st_size <= 0:
            raise SystemExit("QuantBIM desktop publish did not produce QS3D.QuantBim.Desktop.exe")
    print("QuantBIM standalone desktop build/self-test/publish PASS")

print("QuantBIM standalone desktop preflight PASS")
