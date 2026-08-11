#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeImportResolutionPlanner.cs"
READER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeImportResolutionPlannerSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-IMPORT-RESOLUTION-POLICY.md"
errors = []

for path in (PLANNER, READER, SMOKE, REG, DOC):
    if not path.is_file(): errors.append("missing interchange import-resolution contract file: " + str(path.relative_to(ROOT)))

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        "private const int MaxPlanItems = ProjectInterchangeJsonValidator.MaxCollectionItems",
        "ProjectInterchangeValidatedSnapshotReader.Read(json)",
        "InterchangeImportResolutionAction.Unresolved",
        "InterchangeImportResolutionAction.BlockedIncompatible",
        "InterchangeProjectIdPolicy.RequireMatch",
        "InterchangeDrawingFingerprintPolicy.RequireMatch",
        "InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild",
        "import planning has no implicit collision/provenance default",
        "keeping existing generated/native ownership is not an allowed plan",
        "Import resolution refuses ambiguous target identity",
        "public bool CanProceedToMutationDesign",
        ".ToList().AsReadOnly()",
    ):
        if token not in text: errors.append("ProjectInterchangeImportResolutionPlanner.cs missing fail-closed policy token: " + token)

    for enum_name, member in (
        ("InterchangeExistingIdentityAction", r"Unspecified\s*=\s*0"),
        ("InterchangeSourceHandlePolicy", r"PreserveAsProvenanceOnly\s*=\s*2"),
    ):
        pattern = r"enum\s+" + re.escape(enum_name) + r"\s*\{[^}]*\b" + member
        if not re.search(pattern, text, re.DOTALL):
            errors.append("ProjectInterchangeImportResolutionPlanner.cs missing fail-closed enum contract: " + enum_name)

    for token in (
        "targetProject.Zones.Add(",
        "targetProject.Floors.Add(",
        "targetProject.Families.Add(",
        "targetProject.Elements.Add(",
        "targetProject.Name =",
        "targetProject.DrawingFingerprint =",
        "targetProject.Touch(",
        "ProjectStateSnapshot.Restore(",
        ".Erase(",
        "OpenMode.ForWrite",
    ):
        if token in text: errors.append("import resolution planner must remain non-mutating; found: " + token)

if READER.is_file() and "var validation = ProjectInterchangeJsonValidator.Validate(json);" not in READER.read_text(encoding="utf-8"):
    errors.append("validated reader lost validation-first prerequisite")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NoImplicitPolicyDefaultsAreAllowed",
        "AllNewIdentitiesCanBeResolvedWithoutTargetMutation",
        "ExistingElementSourceReplacementRequiresGeneratedReset",
        "KeepTargetDoesNotRequireGeneratedReset",
        "CategoryMismatchIsBlockedRegardlessOfPolicy",
        "ProjectAndFingerprintRequirementsBlockMismatches",
        "SourceHandleDispositionIsExplicitProvenanceOnly",
        "UnsupportedPolicyEnumFailsClosed",
    ):
        if token not in text: errors.append("ProjectInterchangeImportResolutionPlannerSmoke.cs missing scenario: " + token)

if REG.is_file() and "ProjectInterchangeImportResolutionPlannerSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("interchange import-resolution smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "non-mutating policy-resolution layer",
        "No implicit defaults",
        "PreserveAsProvenanceOnly",
        "ClearOwnershipAndRequireRebuild",
        "It does **not** mean import is approved",
        "no `QS3DINTERCHANGEIMPORT`",
    ):
        if token not in text: errors.append("INTERCHANGE-IMPORT-RESOLUTION-POLICY.md missing safety/import boundary: " + token)

print("QS3D interchange import-resolution preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: import resolution requires explicit collision/provenance choices, blocks incompatible identities and generated-output reuse, and remains non-mutating without granting JSON import authority.")
