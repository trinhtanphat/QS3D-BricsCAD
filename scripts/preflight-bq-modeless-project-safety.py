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
        "private readonly string _projectId;",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "_projectId = string.IsNullOrWhiteSpace(project.ProjectId)",
        "SameProjectIdentity(project)",
        "string.Equals(project.ProjectId, _projectId, StringComparison.OrdinalIgnoreCase)",
        "private void EnsureCurrentProject(string operation)",
        "EnsureProjectIdentity(project, operation);",
        "EnsureCurrentProject(\"tính lại BQ\")",
        "EnsureCurrentProject(\"định vị BQ\")",
        "EnsureCurrentProject(\"xuất BQ XLSX\")",
    ):
        if token not in text:
            errors.append("QuantitySummaryWindow.xaml.cs missing modeless project-identity token: " + token)

    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("BQ modeless window must not create/cache replacement project state")
    for forbidden in (
        "private readonly ProjectState _project",
        "private readonly QS3D.Core.Domain.ProjectState _project",
    ):
        if forbidden in text:
            errors.append("BQ modeless window must retain stable ProjectId, not a mutable ProjectState field: " + forbidden)

    ctor_pos = text.find("public QuantitySummaryWindow(")
    ctor_end = text.find("private void ReloadFloors", ctor_pos)
    ctor = text[ctor_pos:ctor_end] if ctor_pos >= 0 and ctor_end > ctor_pos else ""
    read_pos = ctor.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)")
    id_pos = ctor.find("_projectId = string.IsNullOrWhiteSpace(project.ProjectId)")
    init_pos = ctor.find("InitializeComponent();")
    if min(read_pos, id_pos, init_pos) < 0 or not read_pos < id_pos < init_pos:
        errors.append("BQ window must capture existing ProjectId read-only before becoming modeless/initializing callbacks")

    persist_pos = text.find("private void PersistColumnPreferences()")
    persist_end = text.find("private IEnumerable<CheckBox>", persist_pos)
    persist = text[persist_pos:persist_end] if persist_pos >= 0 and persist_end > persist_pos else ""
    bind_pos = persist.find("ExistingProjectMutationContext.TryGet(_document, out var project)")
    identity_pos = persist.find("EnsureProjectIdentity(project, \"lưu cấu hình cột BQ\")")
    snapshot_pos = persist.find("ProjectStateSnapshot.Capture(project)")
    metadata_pos = persist.find("project.Metadata[TemplateProfileStore.VisibleBqColumnsKey]")
    touch_pos = persist.find("project.Touch();")
    if min(bind_pos, identity_pos, snapshot_pos, metadata_pos, touch_pos) < 0 or not bind_pos < identity_pos < snapshot_pos < metadata_pos < touch_pos:
        errors.append("BQ preference write must canonical-bind, verify same ProjectId, snapshot, then mutate metadata/timestamp")

    current_pos = text.find("private void EnsureCurrentProject(string operation)")
    current_end = text.find("private bool SameProjectIdentity", current_pos)
    current = text[current_pos:current_end] if current_pos >= 0 and current_end > current_pos else ""
    if "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" not in current or "EnsureProjectIdentity(project, operation);" not in current:
        errors.append("BQ current-project guard must prove existing state and same ProjectId on every modeless callback")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] BQ modeless callbacks remain DWG-bound and same-ProjectId-bound; reload-safe canonical preference writes retain rollback")
