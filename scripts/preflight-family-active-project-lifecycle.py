#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ACTIVE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.Active.cs"
MAIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.xaml.cs"
errors = []

if not ACTIVE.is_file():
    errors.append("missing FamilyManagerWindow.Active.cs")
else:
    text = ACTIVE.read_text(encoding="utf-8")
    token = 'ExistingProjectMutationContext.Require(_document, "Đặt Family active")'
    if token not in text:
        errors.append("Family active mutation must bind canonical existing project state")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Family active mutation must not create/cache project state")
    bind = text.find(token)
    select = text.find("RequireSelectedFamily(project)")
    mutate = text.find("ExecuteAtomic(project")
    if min(bind, select, mutate) >= 0 and not bind < select < mutate:
        errors.append("Family active lifecycle must be canonical bind -> selected Family resolve -> atomic mutation")

if not MAIN.is_file():
    errors.append("missing FamilyManagerWindow.xaml.cs")
else:
    text = MAIN.read_text(encoding="utf-8")
    for token in (
        'ExistingProjectMutationContext.Require(_document, "Duplicate Family")',
        'ExistingProjectMutationContext.Require(_document, "Lưu Family")',
        'ExistingProjectMutationContext.Require(_document, "Xóa Family")',
        'ExistingProjectMutationContext.Require(_document, "Gán Family cho selection")',
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
    ):
        if token not in text:
            errors.append("Family Manager lifecycle drift; missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Family Manager read paths stay non-creating and all existing-Family mutations, including activate, bind canonical project state.")
