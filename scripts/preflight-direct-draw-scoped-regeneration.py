#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawWindowCommands.cs"
AUTO_HOST = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawCommands.cs")
if not WINDOW.is_file():
    errors.append("missing DirectDrawWindowCommands.cs")
if not AUTO_HOST.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
if not ENGINE.is_file():
    errors.append("missing RegenerationEngine.cs")

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    window = WINDOW.read_text(encoding="utf-8")
    auto_host = AUTO_HOST.read_text(encoding="utf-8")
    engine = ENGINE.read_text(encoding="utf-8")
    start = source.find("private static void ExecuteDirect")
    end = source.find("private static int BuildSelected", start + 1)
    body = source[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append("cannot isolate ExecuteDirect")
    else:
        for token in (
            "configureElement?.Invoke(createdElement)",
            ".RegenerateDirtySubset(project, new[] { createdElement.Id })",
            "solids = BuildSelected(document, project, category)",
            "ProjectStateSnapshot.Capture(project)",
            "rollback.Restore(project)",
        ):
            if token not in body:
                errors.append("Direct Draw scoped-regeneration contract missing: " + token)
        if ".RegenerateDirty(project)" in body:
            errors.append("Direct Draw must not regenerate unrelated dirty project elements")

        configure = body.find("configureElement?.Invoke(createdElement)")
        regen = body.find(".RegenerateDirtySubset(project, new[] { createdElement.Id })")
        build = body.find("solids = BuildSelected(document, project, category)")
        if min(configure, regen, build) < 0 or not (configure < regen < build):
            errors.append("Direct Draw must configure -> scoped-regenerate the created element -> build native geometry")

    draw_start = window.find("public void DrawWindow()")
    draw_end = window.find("private static void Execute(", draw_start + 1)
    draw = window[draw_start:draw_end] if draw_start >= 0 and draw_end > draw_start else ""
    execute_start = draw_end
    execute_end = window.find("private static ProjectState BindProjectAfterPrompts", execute_start + 1)
    execute = window[execute_start:execute_end] if execute_start >= 0 and execute_end > execute_start else ""
    if not draw or not execute:
        errors.append("cannot isolate QS3DDRAWWINDOW prompt/execute lifecycle")
    else:
        for token in (
            "var promptUnit = CadUnitService.GetLengthUnit(document);",
            "var promptUcs = document.Editor.CurrentUserCoordinateSystem;",
            "var expectedProjectId = hasProjectBeforePrompts ? defaultsProject.ProjectId : null;",
            "var expectedProjectChangeVersion = hasProjectBeforePrompts ? (long?)defaultsProject.ChangeVersion : null;",
            'EnsureActive(document, operation + " / prompt freshness")',
            "RequireModelSpace(document);",
            "CurrentUserCoordinateSystem.Equals(promptUcs)",
            "CadUnitService.GetLengthUnit(document) != promptUnit",
            "BindProjectAfterPrompts(document, expectedProjectId, expectedProjectChangeVersion, operation)",
            "Execute(document, project,",
        ):
            if token not in draw:
                errors.append("QS3DDRAWWINDOW freshness contract missing: " + token)

        capture_unit = draw.find("var promptUnit = CadUnitService.GetLengthUnit(document);")
        capture_project = draw.find("var expectedProjectId = hasProjectBeforePrompts ? defaultsProject.ProjectId : null;")
        point_prompt = draw.find("AcquireTwoPoints(document)")
        last_numeric_prompt = draw.find("PromptNonNegativeMeters(document.Editor, \"Khe hở boolean (m)\"")
        active = draw.find('EnsureActive(document, operation + " / prompt freshness")')
        space = draw.find("RequireModelSpace(document);", active)
        ucs = draw.find("CurrentUserCoordinateSystem.Equals(promptUcs)", space)
        unit = draw.find("CadUnitService.GetLengthUnit(document) != promptUnit", ucs)
        bind = draw.find("BindProjectAfterPrompts(document, expectedProjectId, expectedProjectChangeVersion, operation)", unit)
        dispatch = draw.find("Execute(document, project,", bind)
        if min(capture_unit, capture_project, point_prompt, last_numeric_prompt, active, space, ucs, unit, bind, dispatch) < 0 or not (
            capture_unit < point_prompt and capture_project < point_prompt < last_numeric_prompt < active < space < ucs < unit < bind < dispatch
        ):
            errors.append("QS3DDRAWWINDOW must capture unit/project before prompts, then revalidate active DWG/ModelSpace/UCS/unit before exact project bind and execution")

        for token in (
            "RequireExactProject(document, project",
            "regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id })",
            "AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id)",
            "regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id })",
            "rollback.Restore(project)",
        ):
            if token not in execute:
                errors.append("QS3DDRAWWINDOW scoped execution missing: " + token)
        if ".RegenerateDirty(project)" in execute:
            errors.append("QS3DDRAWWINDOW must not regenerate unrelated dirty project elements")
        if "new AutoHostLinkCommands().AutoLinkHosts()" in execute:
            errors.append("QS3DDRAWWINDOW must not call the public Auto Host command wrapper that re-resolves/catches project mutation")
        if "ProjectContextCoordinator.GetOrCreate(document)" in execute:
            errors.append("QS3DDRAWWINDOW Execute must use the exact project bound after prompt freshness validation")

        first_regen = execute.find("regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id })")
        link = execute.find("AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id)")
        host = execute.find("var host = project.FindElement(hostId)", link)
        second_regen = execute.find("regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id })", host)
        if min(first_regen, link, host, second_regen) < 0 or not first_regen < link < host < second_regen:
            errors.append("QS3DDRAWWINDOW must scoped-regenerate opening -> exact Auto Host link -> scoped-regenerate opening+host")

    link_start = auto_host.find("internal static string LinkSingleOpening(")
    link_end = auto_host.find("private static HashSet<string> ReadSelectedHandles", link_start + 1)
    link_body = auto_host[link_start:link_end] if link_start >= 0 and link_end > link_start else ""
    for token in (
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
        "ReferenceEquals(currentProject, project)",
        "new OpeningHostMatcher().Match",
        "new HostLinkService().LinkOpening(project, opening.Id, match.HostElementId)",
    ):
        if token not in link_body:
            errors.append("single-opening Auto Host overload missing exact-project contract: " + token)
    if "RegenerateDirty" in link_body:
        errors.append("single-opening Auto Host overload must leave scoped regeneration to its exact authoring caller")

    if "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)" not in engine:
        errors.append("Core RegenerationEngine no longer exposes targeted regeneration required by Direct Draw")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Direct Draw and QS3DDRAWWINDOW preserve prompt/project freshness and regenerate only the newly authored semantic closure; unrelated dirty project elements stay outside the authoring side effect.")
