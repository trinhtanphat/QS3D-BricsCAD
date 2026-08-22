#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"

GENERATION_FILES = {
    "RebarGeometryCommands.cs": "QS3DREBAR3D",
    "ShapeRebarGeometryCommands.cs": "QS3DREBAR3DSHAPE",
    "BeamRebarCommands.cs": "QS3DBEAMREBAR3D",
    "BeamStirrupCommands.cs": "QS3DREBARSTIRRUP3D",
    "ColumnTieCommands.cs": "QS3DREBARTIES3D",
    "SlabMeshCommands.cs": "QS3DSLABREBAR3D",
    "StructuralWallMeshCommands.cs": "QS3DWALLREBAR3D",
    "FoundationMeshCommands.cs": "QS3DFOUNDATIONREBAR3D",
    "RebarMeshSetupCommands.cs": "QS3DREBARMESHSETUP",
}

errors = []
for filename, command in GENERATION_FILES.items():
    path = SRC / filename
    if not path.is_file():
        errors.append("missing " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    if 'CommandMethod("' + command + '"' not in text:
        errors.append(filename + ": missing " + command)
    if "ExistingProjectMutationContext.Require(document" not in text:
        errors.append(filename + ": semantic rebar generation/setup must bind canonical existing project")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + ": semantic rebar generation/setup must not create/cache project state directly")

for filename, health_command in (
    ("BeamStirrupCommands.cs", "QS3DREBARSTIRRUPHEALTH"),
    ("SlabMeshCommands.cs", "QS3DSLABREBARHEALTH"),
):
    text = (SRC / filename).read_text(encoding="utf-8")
    marker = 'CommandMethod("' + health_command + '"'
    start = text.find(marker)
    if start < 0:
        errors.append(filename + ": missing " + health_command)
        continue
    health = text[start:]
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in health:
        errors.append(filename + ": Health path must remain read-only")

setup = (SRC / "RebarMeshSetupCommands.cs").read_text(encoding="utf-8")
if "if (snapshots.Count == 0) return;" not in setup:
    errors.append("Rebar Mesh Setup must remain side-effect free on empty/cancelled selection")
if "var elementId = element.Id;" not in setup:
    errors.append("Rebar Mesh Setup post-save UI callback should retain stable ElementId rather than semantic element")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic rebar generation/setup binds canonical existing project state while embedded Health paths remain read-only.")
