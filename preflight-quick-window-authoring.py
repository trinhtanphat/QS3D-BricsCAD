#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawWindowCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-WINDOW-2026-08-11.md"
errors = []

for path in (SOURCE, RIBBON, DOC):
    if not path.is_file():
        errors.append("missing quick-window dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    for name in ("QS3DDRAWWINDOW", "QS3DDRAWWINDOWADV"):
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

    for token in (
        'DrawWindowCore(promptParameters: false, operation: "QS3DDRAWWINDOW")',
        'DrawWindowCore(promptParameters: true, operation: "QS3DDRAWWINDOWADV")',
        'FamilyWindowNumber(defaultsProject, "WindowHeightM", 1.2d, positive: true)',
        'FamilyWindowNumber(defaultsProject, "WindowSillHeightM", 0.9d, positive: false)',
        'FamilyWindowNumber(defaultsProject, "BooleanClearanceM", 0.01d, positive: false)',
        "if (promptParameters)",
        'PromptPositiveMeters(document.Editor, "Chiều cao Cửa Sổ (m)"',
        'PromptNonNegativeMeters(document.Editor, "Cao độ bậu Cửa Sổ so với đáy host (m)"',
        'PromptNonNegativeMeters(document.Editor, "Khe hở boolean (m)"',
        "QS3DDRAWWINDOWADV",
        'createdElement.SetProperty("OpeningUsage", "Window")',
        "new AutoHostLinkCommands().AutoLinkHosts()",
        'createdElement.Properties.TryGetValue("HostWallId"',
        "ProjectStateSnapshot.Capture(project)",
        "EraseSource(document, sourceId)",
        "rollback.Restore(project)",
    ):
        if token not in text:
            errors.append("quick Window contract missing: " + token)

    defaults = text.find("var heightDefault = hasProject")
    gate = text.find("if (promptParameters)")
    else_pos = text.find("else", gate)
    execute = text.find("Execute(document, points[0], points[1], widthM, heightM, sillM, clearanceM)", else_pos)
    if min(defaults, gate, else_pos, execute) < 0 or not (defaults < gate < else_pos < execute):
        errors.append("Window must resolve defaults -> optional advanced prompts -> canonical Execute")
    else:
        quick_body = text[else_pos:execute]
        if "PromptPositiveMeters(" in quick_body or "PromptNonNegativeMeters(" in quick_body:
            errors.append("primary Window quick branch must not prompt numeric parameters")
        if "QS3DDRAWWINDOWADV" not in quick_body:
            errors.append("primary Window quick status must advertise QS3DDRAWWINDOWADV")

    if "OpeningBooleanService.CutLinkedOpenings" in text or "QS3DCUTOPENINGS " in text:
        errors.append("Window Direct Draw must keep physical boolean explicit")

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    if 'new ButtonSpec("QS3D_AUTHOR_WINDOW", "Vẽ Cửa Sổ", "QS3DDRAWWINDOW")' not in text:
        errors.append("primary Window Ribbon action must continue to invoke QS3DDRAWWINDOW")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWWINDOWADV",
        "Family / Type",
        "Auto Host",
        "physical boolean",
        "LOCAL-008",
        "OpeningUsage=Window",
    ):
        if token not in text:
            errors.append("quick Window documentation missing: " + token)

if errors:
    print("Quick Window authoring preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Quick Window authoring preflight PASS")
