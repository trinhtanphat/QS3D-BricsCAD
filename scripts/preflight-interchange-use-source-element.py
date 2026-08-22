#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
service = root / "src/QS3D.BricsCAD.V25/Services/InterchangeUseSourceElementImportService.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeUseSourceCommands.cs"
append_command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs"
project_tools = root / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"

errors = []
for path in (service, command, append_command, project_tools):
    if not path.exists():
        errors.append(f"missing required source: {path.relative_to(root)}")

if not errors:
    s = service.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")
    a = append_command.read_text(encoding="utf-8")
    ui = project_tools.read_text(encoding="utf-8")

    required_service = [
        "ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData",
        "SourceHandles = InterchangeSourceHandlePolicy.Discard",
        "GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild",
        "using (document.LockDocument())",
        "var lockedProject = InterchangeMutationTargetGuard.RequireExact(",
        "var lockedReplacementTargets = prepared.Plan.ReplacementElementIds",
        "var lockedInvalidationTargets = ExpandInvalidationTargets(",
        "rollback = ProjectStateSnapshot.Capture(lockedProject)",
        "GeneratedDependentGeometryInvalidator.Prepare(",
        "lockedProject,",
        "lockedInvalidationTargets);",
        "ApplyCatalogAdds(lockedProject, prepared.Source, prepared.Resolution)",
        "ApplyElements(lockedProject, prepared.Source, prepared.Resolution)",
        "invalidation.CommitMetadata();",
        "transaction.Commit();",
        "rollback.Restore(project)",
        "ExpandInvalidationTargets",
        "GetDirectDependents",
        "HostWallId",
        "target.Properties.Clear();",
        "target.Quantities.Clear();",
        "target.MarkDirty(ElementDirtyFlags.All);",
        "ProjectInterchangeKeepTargetImporter.Plan(project, json)",
        "ProjectContextCoordinator.RequireBackingStoreUnchanged(",
    ]
    for needle in required_service:
        if needle not in s:
            errors.append(f"service missing atomic/policy contract: {needle}")

    prepare_index = s.find("GeneratedDependentGeometryInvalidator.Prepare")
    commit_index = s.find("transaction.Commit();")
    if min(prepare_index, commit_index) < 0 or prepare_index > commit_index:
        errors.append("locked native generated-output invalidation must be prepared before CAD commit")
    if s.index("invalidation.CommitMetadata();") > s.index("transaction.Commit();"):
        errors.append("semantic generated-output ownership must clear before CAD commit")
    if s.count("ProjectStateSnapshot.Capture(lockedProject)") != 1:
        errors.append("element service must own exactly one locked semantic rollback snapshot")

    forbidden_service = [
        "QS3DBUILD3D",
        ".SourceHandles.Clear()",
        ".SourceHandles.Add(",
        "target.DrawingFingerprint = source.DrawingFingerprint",
        "target.DrawingFingerprint = snapshot.DrawingFingerprint",
    ]
    for needle in forbidden_service:
        if needle in s:
            errors.append(f"service crosses protected source/rebuild boundary: {needle}")

    if c.count('[CommandMethod("QS3DINTERCHANGEUSESOURCE"') != 1:
        errors.append("QS3DINTERCHANGEUSESOURCE must be registered exactly once in its command source")
    for needle in [
        "InterchangeUseSourceElementImportService.Plan(project, json)",
        "InterchangeUseSourceElementImportService.Import(document, confirmedProject, json)",
        "MessageBoxButton.YesNo",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "Không tự chạy QS3DBUILD3D",
    ]:
        if needle not in c:
            errors.append(f"command missing guarded UX contract: {needle}")

    if "QS3DINTERCHANGEUSESOURCE" in a:
        errors.append("append-only command source must remain separate from UseSource replacement command")
    if "UseSourceSemanticData" in a:
        errors.append("QS3DINTERCHANGEAPPEND must not silently acquire replacement semantics")

    if ui.count('Tag="QS3DINTERCHANGEUSESOURCE"') != 1:
        errors.append("Project Tools must expose QS3DINTERCHANGEUSESOURCE exactly once")
    for needle in [
        "Nạp Snapshot (Append-only)",
        "Nạp Snapshot (Replace Element semantic)",
        "giữ source CAD ownership của target",
        "yêu cầu rebuild explicit",
    ]:
        if needle not in ui:
            errors.append(f"Project Tools missing separated interchange UX contract: {needle}")

    all_cs = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEUSESOURCE"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEUSESOURCE command registration count must be 1, got {registrations}")

if errors:
    print("preflight-interchange-use-source-element: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-use-source-element: PASS")
print("UseSource Element import rebinds the exact locked project before native invalidation + semantic rollback; target source ownership is preserved, UI keeps Append separate, and rebuild remains explicit.")
