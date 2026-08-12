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
        "var projectPreview = DirectDrawProjectPreviewContext.Capture(document);",
        "var defaultsProject = projectPreview.DefaultsProject;",
        "var hasProjectBeforePrompts = projectPreview.HasProject;",
        "var expectedProjectChangeVersion = hasProjectBeforePrompts ? (long?)defaultsProject!.ChangeVersion : null;",
        'FamilyWindowNumber(defaultsProject!, "WindowHeightM", 1.2d, positive: true)',
        'FamilyWindowNumber(defaultsProject!, "WindowSillHeightM", 0.9d, positive: false)',
        'FamilyWindowNumber(defaultsProject!, "BooleanClearanceM", 0.01d, positive: false)',
        "if (promptParameters)",
        'PromptPositiveMeters(document.Editor, "Chiều cao Cửa Sổ (m)"',
        'PromptNonNegativeMeters(document.Editor, "Cao độ bậu Cửa Sổ so với đáy host (m)"',
        'PromptNonNegativeMeters(document.Editor, "Khe hở boolean (m)"',
        "QS3DDRAWWINDOWADV",
        'EnsureActive(document, operation + " / prompt freshness")',
        "if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))",
        "if (CadUnitService.GetLengthUnit(document) != promptUnit)",
        "var project = BindProjectAfterPrompts(document, projectPreview, expectedProjectChangeVersion, operation);",
        "Execute(document, project, hasProjectBeforePrompts, points[0], points[1], widthM, heightM, sillM, clearanceM);",
        "projectPreview.ResolveForMutation(document, operation)",
        "project.ChangeVersion != expectedProjectChangeVersion.Value",
        'createdElement.SetProperty("OpeningUsage", "Window")',
        "AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id)",
        'createdElement.Properties.TryGetValue("HostWallId"',
        "ProjectStateSnapshot.Capture(project)",
        "EraseSource(document, sourceId)",
        "rollback.Restore(project)",
        "if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);",
    ):
        if token not in text:
            errors.append("quick Window contract missing: " + token)

    defaults = text.find("var heightDefault = hasProjectBeforePrompts")
    gate = text.find("if (promptParameters)")
    else_pos = text.find("else", gate)
    freshness = text.find('EnsureActive(document, operation + " / prompt freshness")', else_pos)
    bind = text.find("var project = BindProjectAfterPrompts", freshness)
    execute = text.find("Execute(document, project, hasProjectBeforePrompts, points[0], points[1], widthM, heightM, sillM, clearanceM);", bind)
    if min(defaults, gate, else_pos, freshness, bind, execute) < 0 or not (defaults < gate < else_pos < freshness < bind < execute):
        errors.append("Window must resolve defaults -> optional advanced prompts -> prompt freshness -> canonical project bind -> Execute")
    else:
        quick_body = text[else_pos:freshness]
        if "PromptPositiveMeters(" in quick_body or "PromptNonNegativeMeters(" in quick_body:
            errors.append("primary Window quick branch must not prompt numeric parameters")
        if "QS3DDRAWWINDOWADV" not in quick_body:
            errors.append("primary Window quick status must advertise QS3DDRAWWINDOWADV")

    bind_start = text.find("private static ProjectState BindProjectAfterPrompts")
    exact_start = text.find("private static void RequireExactProject", bind_start)
    if min(bind_start, exact_start) < 0:
        errors.append("Window prompt/project freshness helpers are missing")
    else:
        bind_body = text[bind_start:exact_start]
        resolve = bind_body.find("projectPreview.ResolveForMutation(document, operation)")
        version = bind_body.find("project.ChangeVersion != expectedProjectChangeVersion.Value")
        if min(resolve, version) < 0 or resolve > version:
            errors.append("Window must bind the post-prompt project before validating expected ChangeVersion")

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

print("Quick Window authoring preflight PASS: quick/advanced prompts retain exact behavior, prompt/UCS/unit/project freshness is validated before canonical mutation binding, and rollback/Auto Host remain scoped to the authored Window.")
