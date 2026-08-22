#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
HUB = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
DOC = ROOT / "docs/DIRECT-DRAW-QUICK-OPENINGS-2026-08-11.md"
errors = []

for path in (SOURCE, RIBBON, HUB, DOC):
    if not path.is_file():
        errors.append("missing quick-opening dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    commands = re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
    for name in ("QS3DDRAWDOOR", "QS3DDRAWDOORADV", "QS3DDRAWOPENING", "QS3DDRAWOPENINGADV"):
        if commands.count(name) != 1:
            errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

    wrapper_contracts = (
        'DrawOpening(ElementCategory.Door, "Cửa Đi", defaultSillM: 0d, promptParameters: false, operation: "QS3DDRAWDOOR")',
        'DrawOpening(ElementCategory.Door, "Cửa Đi", defaultSillM: 0d, promptParameters: true, operation: "QS3DDRAWDOORADV")',
        'DrawOpening(ElementCategory.WallOpening, "Lỗ Mở Vách", defaultSillM: 0d, promptParameters: false, operation: "QS3DDRAWOPENING")',
        'DrawOpening(ElementCategory.WallOpening, "Lỗ Mở Vách", defaultSillM: 0d, promptParameters: true, operation: "QS3DDRAWOPENINGADV")',
    )
    for token in wrapper_contracts:
        if token not in text:
            errors.append("quick-opening wrapper contract missing: " + token)

    defaults = text.find("var heightDefault = hasDefaultsProject")
    prompt_gate = text.find("if (promptParameters)")
    execute = text.find("Execute(document, category, label, points[0], points[1], widthM, heightM, sillM, clearanceM, projectPreview);")
    if min(defaults, prompt_gate, execute) < 0 or not (defaults < prompt_gate < execute):
        errors.append("Door/Opening must resolve defaults -> optional advanced prompts -> canonical Execute")

    prompt_body_end = text.find("else", prompt_gate)
    if prompt_body_end < 0:
        errors.append("Door/Opening quick/advanced prompt branch is incomplete")
    else:
        prompt_body = text[prompt_gate:prompt_body_end]
        for token in ("PromptPositiveMeters(", "PromptNonNegativeMeters("):
            if token not in prompt_body:
                errors.append("advanced Door/Opening branch missing prompt helper: " + token)

    quick_else_start = prompt_body_end
    quick_else_end = text.find("Execute(document, category", quick_else_start)
    quick_body = text[quick_else_start:quick_else_end] if quick_else_end > quick_else_start else ""
    if "PromptPositiveMeters(" in quick_body or "PromptNonNegativeMeters(" in quick_body:
        errors.append("primary Door/Opening quick branch must not prompt numeric parameters")
    for token in ("QS3DDRAWDOORADV", "QS3DDRAWOPENINGADV"):
        if token not in quick_body:
            errors.append("quick Door/Opening status must advertise advanced command: " + token)

    for token in (
        'FamilyPositiveNumber(defaultsProject!, category, "HeightM", 2.2d)',
        'FamilyNonNegativeNumber(defaultsProject!, category, "BottomOffsetM", defaultSillM)',
        'FamilyNonNegativeNumber(defaultsProject!, category, "SillHeightM", bottomOffsetDefault)',
        'FamilyNonNegativeNumber(defaultsProject!, category, "BooleanClearanceM", 0.01d)',
        "new AutoHostLinkCommands().AutoLinkHosts()",
        'createdElement.Properties.TryGetValue("HostWallId"',
        'createdElement.SetProperty("WidthM"',
        'createdElement.SetProperty("HeightM"',
        'createdElement.SetProperty("SillHeightM"',
        'createdElement.SetProperty("BooleanClearanceM"',
        "EraseSource(document, sourceId)",
        "rollback.Restore(project)",
    ):
        if token not in text:
            errors.append("quick Door/Opening lost guarded lifecycle token: " + token)

    if "OpeningBooleanService.CutLinkedOpenings" in text or "SendStringToExecute(\"QS3DCUTOPENINGS" in text:
        errors.append("Door/Opening Direct Draw must keep physical boolean explicit")

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    for token in (
        'Button("Vẽ Cửa", "QS3DDRAWDOOR")',
        'Button("Vẽ Lỗ Mở", "QS3DDRAWOPENING")',
    ):
        if token not in text:
            errors.append("primary quick opening Ribbon wiring missing: " + token)

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    for command in ("QS3DDRAWDOOR", "QS3DDRAWOPENING"):
        if 'Tag="' + command + '"' not in text:
            errors.append("Domain Hub missing primary quick opening command: " + command)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DDRAWDOORADV",
        "QS3DDRAWOPENINGADV",
        "Auto Host",
        "physical boolean",
        "LOCAL-008",
        "Family / Type",
    ):
        if token not in text:
            errors.append("quick-opening documentation missing: " + token)

if errors:
    print("Quick Door/Opening authoring preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Quick Door/Opening authoring preflight PASS")
