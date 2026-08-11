#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
GUARD = ADAPTER / "Cad" / "CadSelectionGuard.cs"
BEAM = ADAPTER / "BeamRebarCommands.cs"
STIRRUP = ADAPTER / "BeamStirrupCommands.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def region(text, start_token, end_token, label):
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end <= start:
        errors.append("cannot isolate " + label)
        return ""
    return text[start:end]


guard = read(GUARD)
beam = read(BEAM)
stirrup = read(STIRRUP)

for token in (
    "SelectImplied()",
    "GetSelection()",
    "SetImpliedSelection(objectIds)",
    "return Array.Empty<ObjectId>();",
):
    if token not in guard:
        errors.append("CadSelectionGuard missing PICKFIRST/cancel contract token: " + token)

for forbidden in (
    "ProjectContextCoordinator",
    "ExistingProjectMutationContext",
    "ProjectState",
):
    if forbidden in guard:
        errors.append("CadSelectionGuard must remain project-agnostic: " + forbidden)

cases = (
    (
        beam,
        "public void BuildBeamRebar3D()",
        "private static void FinalizeUi",
        "Beam Rebar",
        "ExistingProjectMutationContext.Require(document, \"Beam Rebar 3D\")",
        "BeamRebarSolidBuilder.BuildSelected(document, project)",
    ),
    (
        stirrup,
        "public void BuildBeamStirrups()",
        "[CommandMethod(\"QS3DBEAMSTIRRUPHEALTH\"",
        "Beam Stirrup",
        "ExistingProjectMutationContext.Require(document, \"Beam Stirrup 3D\")",
        "BeamStirrupSolidBuilder.BuildSelected(document, project)",
    ),
)

for text, start_token, end_token, label, require_token, build_token in cases:
    body = region(text, start_token, end_token, label)
    acquire = body.find("CadSelectionGuard.AcquireCurrentSelection(document)")
    empty = body.find("if (selectedIds.Length == 0)")
    require = body.find(require_token)
    build = body.find(build_token)
    if min(acquire, empty, require, build) < 0:
        errors.append(label + " command missing selection/project/build lifecycle token")
        continue
    if not (acquire < empty < require < build):
        errors.append(label + " must acquire selection and return on empty/cancel before canonical project binding/build")
    if "ProjectContextCoordinator.GetOrCreate" in body:
        errors.append(label + " mutation command must not create a replacement project")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Beam longitudinal/stirrup 3D commands acquire PICKFIRST/interactive selection before binding the existing canonical project, preserving one-prompt builder behavior and cancel/empty fail-closed semantics.")
