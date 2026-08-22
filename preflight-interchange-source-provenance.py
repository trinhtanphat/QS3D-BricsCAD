#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
store = root / "src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeProvenanceCommands.cs"
exporter = root / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
ownership = root / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
project_tools = root / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"

errors = []
for path in (store, command, exporter, ownership, project_tools):
    if not path.exists():
        errors.append(f"missing required source: {path.relative_to(root)}")

if not errors:
    s = store.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")
    e = exporter.read_text(encoding="utf-8")
    o = ownership.read_text(encoding="utf-8")
    ui = project_tools.read_text(encoding="utf-8")

    required_store = [
        'MetadataPrefix = "Interchange.Provenance.Source."',
        'PolicyName = "PreserveAsProvenanceOnly"',
        "InterchangeSourceHandlePolicy.PreserveAsProvenanceOnly.ToString()",
        "ProjectStateSnapshot.Capture(target)",
        "rollback.Restore(target)",
        "ProjectInterchangeValidatedSnapshotReader.Read(json)",
        "target.Metadata[sourcePrefix + ProjectRecordSuffix]",
        "target.Metadata[sourcePrefix + ElementRecordSegment + Token(element.Id)]",
        "No imported handle was assigned to target DWG ownership",
        "ToUpperInvariant()",
        "target.Touch();",
    ]
    for needle in required_store:
        if needle not in s:
            errors.append(f"provenance store missing contract: {needle}")

    forbidden_store = [
        ".SourceHandles.Add(",
        ".SourceHandles.Clear()",
        'Properties["Generated',
        "GeneratedDependentGeometryInvalidator",
        "QS3DBUILD3D",
    ]
    for needle in forbidden_store:
        if needle in s:
            errors.append(f"provenance-only store crosses native ownership boundary: {needle}")

    if "project.Metadata" in e or "project.Metadata".lower() in e.lower():
        errors.append("semantic snapshot exporter must not serialize Project.Metadata provenance records")
    if "Interchange.Provenance" in o:
        errors.append("generated ownership scanner must not treat interchange provenance as owner slots")

    required_command = [
        '[CommandMethod("QS3DINTERCHANGEPROVENANCE", CommandFlags.Modal)]',
<<<<<<< HEAD
        "ProjectInterchangeSourceHandleProvenance.Plan(reviewProject, json)",
=======
        "ProjectContextCoordinator.TryGetReadOnly(document, out var reviewProject)",
        "ProjectInterchangeSourceHandleProvenance.Plan(reviewProject, json)",
        "var reviewProjectId = reviewProject.ProjectId;",
        "var reviewUpdatedUtc = reviewProject.UpdatedUtc;",
        "var reviewChangeVersion = reviewProject.ChangeVersion;",
        "var reviewDrawingFingerprint = reviewProject.DrawingFingerprint ?? string.Empty;",
        'ExistingProjectMutationContext.Require(document, "Interchange provenance import")',
>>>>>>> origin/main
        "ProjectInterchangeSourceHandleProvenance.Store(project, json)",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        "MessageBoxButton.YesNo",
        "KHÔNG ghi handle vào ProjectElement.SourceHandles",
        "EnsureActive(document",
    ]
    for needle in required_command:
        if needle not in c:
            errors.append(f"provenance command missing guarded UX/freshness contract: {needle}")
    if "ProjectContextCoordinator.GetOrCreate(document)" in c:
        errors.append("provenance review/commit must not create/cache replacement project state")

    plan = c.find("ProjectInterchangeSourceHandleProvenance.Plan(reviewProject, json)")
    confirm = c.find("MessageBoxButton.YesNo", plan if plan >= 0 else 0)
    bind = c.find('ExistingProjectMutationContext.Require(document, "Interchange provenance import")')
    store_pos = c.find("ProjectInterchangeSourceHandleProvenance.Store(project, json)")
    if min(plan, confirm, bind, store_pos) < 0 or not plan < confirm < bind < store_pos:
        errors.append("provenance command must review/plan first, confirm, rebind canonical existing state, validate snapshot stamps, then store")

    all_cs = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEPROVENANCE"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEPROVENANCE command registration count must be 1, got {registrations}")

    if ui.count('Tag="QS3DINTERCHANGEPROVENANCE"') != 1:
        errors.append("Project Tools must expose QS3DINTERCHANGEPROVENANCE exactly once")
    for needle in ["Lưu provenance source handles", "không nhận CAD ownership"]:
        if needle not in ui:
            errors.append(f"Project Tools missing provenance-only UX boundary: {needle}")

if errors:
    print("preflight-interchange-source-provenance: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-source-provenance: PASS")
print("Imported source handles are reviewed on a read-only snapshot, committed only after canonical existing-project rebind/freshness validation, stored solely as provenance metadata, and never become target DWG ownership.")
