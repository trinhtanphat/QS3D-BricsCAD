#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ZONE = ROOT / "src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs"
FAMILY = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs"
FAMILY_ACTIVE = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.Active.cs"
errors = []

for path in (ZONE, FAMILY, FAMILY_ACTIVE):
    if not path.is_file():
        errors.append("missing Zone/Family refresh identity file: " + str(path.relative_to(ROOT)))


def block(text, start_token, end_token):
    start = text.find(start_token)
    end = text.find(end_token, start + len(start_token))
    if start < 0 or end < 0:
        return ""
    return text[start:end]


def require_guard(text, manager, mutation_methods):
    for token in (
        "private ProjectState? _boundProject;",
        "_boundProject = null;",
        "_boundProject = project;",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject)",
        "_boundProject == null",
        "!ReferenceEquals(currentProject, _boundProject)",
    ):
        if token not in text:
            errors.append(manager + " missing canonical refresh identity token: " + token)

    ensure = text.find("private void EnsureActive(string operation)")
    if ensure < 0:
        errors.append(manager + " missing EnsureActive lifecycle guard")
    else:
        ensure_block = text[ensure:]
        for token in (
            "Application.DocumentManager.MdiActiveDocument",
            "ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject)",
            "!ReferenceEquals(currentProject, _boundProject)",
            "Refresh " + manager,
        ):
            if token not in ensure_block:
                errors.append(manager + " EnsureActive missing stale-project guard token: " + token)

    for method, next_method in mutation_methods:
        method_block = block(text, method, next_method)
        if not method_block:
            errors.append(manager + " mutation boundary missing: " + method)
            continue
        ensure_call = method_block.find("EnsureActive(")
        mutation_bind = method_block.find("ExistingProjectMutationContext.Require(_document")
        if ensure_call < 0:
            errors.append(method + " must pass the canonical project guard")
        if mutation_bind >= 0 and not ensure_call < mutation_bind:
            errors.append(method + " must guard stale project identity before mutation binding")


if ZONE.is_file():
    text = ZONE.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Zone Manager modeless callbacks must not create/cache replacement project state")
    require_guard(text, "Zone Manager", (
        ("private void OnSaveClick", "private void OnDeleteClick"),
        ("private void OnDeleteClick", "private void OnActivateClick"),
        ("private void OnActivateClick", "private void OnAssignClick"),
        ("private void OnAssignClick", "private void OnInspectClick"),
    ))
    refresh = block(text, "private void RefreshAll", "private ZoneDefinition RequireSelectedZone")
    if not refresh:
        errors.append("Zone Manager RefreshAll boundary missing")
    else:
        clear = refresh.find("_boundProject = null;")
        load = refresh.find("LoadEditor();")
        labels = refresh.find("RefreshLabels();")
        bind = refresh.find("_boundProject = project;")
        if min(clear, load, labels, bind) < 0 or not load < labels < bind:
            errors.append("Zone Manager RefreshAll must clear unavailable binding and synchronize editor/labels before rebinding canonical project")

if FAMILY.is_file():
    text = FAMILY.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Family Manager modeless callbacks must not create/cache replacement project state")
    require_guard(text, "Family Manager", (
        ("private void OnDuplicateClick", "private void OnRenameClick"),
        ("private void OnRenameClick", "private void OnDeleteClick"),
        ("private void OnDeleteClick", "private void OnSavePropertyClick"),
        ("private void OnSavePropertyClick", "private void OnRemovePropertyClick"),
        ("private void OnRemovePropertyClick", "private void OnAssignClick"),
        ("private void OnAssignClick", "private void RefreshAll"),
    ))
    refresh = block(text, "private void RefreshAll", "private void LoadFamily")
    if not refresh:
        errors.append("Family Manager RefreshAll boundary missing")
    else:
        clear = refresh.find("_boundProject = null;")
        load = refresh.find("LoadFamily();")
        bind = refresh.find("_boundProject = project;")
        if min(clear, load, bind) < 0 or not load < bind:
            errors.append("Family Manager RefreshAll must clear unavailable binding and synchronize editor before rebinding canonical project")

if FAMILY_ACTIVE.is_file():
    text = FAMILY_ACTIVE.read_text(encoding="utf-8")
    active = block(text, "private void OnActivateClick", "    }\n}")
    if not active:
        active = text[text.find("private void OnActivateClick"):]
    ensure = active.find("EnsureActive(")
    mutation_bind = active.find("ExistingProjectMutationContext.Require(_document")
    if ensure < 0 or mutation_bind < 0 or not ensure < mutation_bind:
        errors.append("Family Manager active mutation must pass stale-project guard before canonical mutation binding")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Zone/Family modeless managers bind canonical project identity on Refresh, fail closed on replacement before mutations, and retain existing mutation binding boundaries")
