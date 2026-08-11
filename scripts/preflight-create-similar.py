#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CreateSimilarCommands.cs"
ACTIVE = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs"
DOC = ROOT / "docs/DIRECT-DRAW-CREATE-SIMILAR-2026-08-11.md"
errors = []

for path in (SOURCE, ACTIVE, RIBBON, DOC):
    if not path.is_file():
        errors.append("missing Create Similar dependency: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for command in ("QS3DCREATESIMILAR", "QS3DCREATESIMILARADV"):
        if len(re.findall(r'CommandMethod\("' + command + r'"', text)) != 1:
            errors.append(command + " must be declared exactly once")

    for token in (
        'CreateSimilarCore(advanced: false, operation: "QS3DCREATESIMILAR")',
        'CreateSimilarCore(advanced: true, operation: "QS3DCREATESIMILARADV")',
        "document.Editor.GetEntity(new PromptEntityOptions(message))",
        "if (result.Status != PromptStatus.OK) return null;",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "ResolveOwner(previewProject, selectedHandle)",
        "ResolveFamily(previewProject, previewOwner.Element)",
        "ActiveFamilyQuickDrawCommands.SupportsFamily(previewFamily)",
        "var expectedProjectId = previewProject.ProjectId;",
        "var expectedChangeVersion = previewProject.ChangeVersion;",
        "var expectedOwnerId = previewOwner.Element.Id;",
        "var expectedFamilyId = previewFamily.Id;",
        "var expectedCategory = previewFamily.Category;",
        "var expectedOwnerKind = previewOwner.Kind;",
        "var expectedOwnerSlot = previewOwner.OwnerSlot;",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
        "ExistingProjectMutationContext.Require(document, operation)",
        "project.ChangeVersion != expectedChangeVersion",
        "ResolveOwner(project, selectedHandle)",
        "ResolveFamily(project, currentOwner.Element)",
        "ActiveFamilyQuickDrawCommands.SupportsFamily(currentFamily)",
        "ProjectFamilyActivationService.SetActive(project, currentFamily.Id);",
        "dispatcher.DrawActiveFamilyAdvanced();",
        "dispatcher.DrawActiveFamily();",
        "GeneratedHandleOwnershipPolicy.TryFindOwner(project, normalized",
        "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(generatedSlot)",
        "element.SourceHandles.Any(source =>",
        "candidates.Count != 1",
    ):
        if token not in text:
            errors.append("Create Similar source contract missing: " + token)

    prompt = text.find("var selectedHandle = PromptEntityHandle")
    cancel = text.find("if (selectedHandle == null) return;", prompt)
    readonly = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)", cancel)
    freeze = text.find("var expectedProjectId = previewProject.ProjectId;", readonly)
    bind = text.find("ExistingProjectMutationContext.Require(document, operation)", freeze)
    re_resolve = text.find("ResolveOwner(project, selectedHandle)", bind)
    activate = text.find("ProjectFamilyActivationService.SetActive(project, currentFamily.Id);", re_resolve)
    dispatch = text.find("var dispatcher = new ActiveFamilyQuickDrawCommands();", activate)
    if min(prompt, cancel, readonly, freeze, bind, re_resolve, activate, dispatch) < 0 or not (
        prompt < cancel < readonly < freeze < bind < re_resolve < activate < dispatch
    ):
        errors.append("Create Similar must select/cancel, read-only preview, freeze, canonical-bind, re-resolve, activate, then delegate in that order")

    if text.count("ActiveFamilyQuickDrawCommands.SupportsFamily(") != 2:
        errors.append("Create Similar must reject unsupported Family categories both before and after canonical re-resolution")
    if "case ElementCategory." in text:
        errors.append("Create Similar must not carry a second category dispatch/support switch")

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "SendStringToExecute",
        "new DirectDrawCommands",
        "new DirectDrawP1Commands",
        "new DirectDrawOpeningCommands",
        "new DirectDrawWindowCommands",
        "SemanticCaptureService.Capture",
        "RegenerationEngine",
        "WallSolidBuilder",
        "StructuralSolidBuilder",
    ):
        if forbidden in text:
            errors.append("Create Similar must not duplicate project bootstrap/authoring lifecycle: " + forbidden)

if ACTIVE.is_file():
    text = ACTIVE.read_text(encoding="utf-8")
    for token in (
        "internal static bool SupportsFamily(ProjectFamily family)",
        "if (!SupportsFamily(family))",
        "case ElementCategory.ArchitecturalWall:",
        "case ElementCategory.Beam:",
        "case ElementCategory.Column:",
        "case ElementCategory.Slab:",
        "case ElementCategory.GlassWall:",
        "case ElementCategory.WallPier:",
        "case ElementCategory.StructuralWall:",
        "case ElementCategory.Foundation:",
        "case ElementCategory.Door:",
        "case ElementCategory.WallOpening:",
    ):
        if token not in text:
            errors.append("Active Family shared support contract missing: " + token)

    support_start = text.find("internal static bool SupportsFamily(ProjectFamily family)")
    support_end = text.find("private static void DrawActiveFamilyCore", support_start)
    dispatch_start = text.find("private static void Dispatch(")
    dispatch_end = text.find("private static bool IsWindowFamily", dispatch_start)
    if min(support_start, support_end, dispatch_start, dispatch_end) < 0:
        errors.append("could not isolate Active Family support/dispatch sections")
    else:
        case_pattern = re.compile(r"case\s+ElementCategory\.([A-Za-z0-9_]+)\s*:")
        supported = set(case_pattern.findall(text[support_start:support_end]))
        dispatched = set(case_pattern.findall(text[dispatch_start:dispatch_end]))
        if supported != dispatched:
            errors.append(
                "Active Family support predicate drifted from dispatcher categories: supported=" +
                ",".join(sorted(supported)) + " dispatched=" + ",".join(sorted(dispatched)))

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    button = 'new ButtonSpec("QS3D_AUTHOR_CREATE_SIMILAR", "Vẽ Tương Tự", "QS3DCREATESIMILAR")'
    if text.count(button) != 1:
        errors.append("Quick Workflow Ribbon must contain exactly one stable Vẽ Tương Tự/Create Similar button")
    for token in (
        'private const string PanelSourceId = "QS3D_AUTHOR_QUICK_PANEL_SOURCE";',
        'private const string PanelTitle = "Tác vụ nhanh";',
        "FindPanelSource(panelItems, PanelSourceId) ?? CreateQuickPanel(panels)",
        "private static object? FindPanelSource(IEnumerable panels, string sourceId)",
        "private static object CreateQuickPanel(object panels)",
        'Create("Bricscad.Windows.RibbonPanelSource")',
        'SetProperty(source, "Id", PanelSourceId);',
        'SetProperty(source, "Title", PanelTitle);',
        'Create("Bricscad.Windows.RibbonPanel")',
        'SetProperty(panel, "Source", source);',
        "Add(panels, panel);",
        "var button = FindById(items, spec.Id);",
        "if (button == null)",
        'button = Create("Bricscad.Windows.RibbonButton");',
        'SetProperty(button, "Id", spec.Id);',
        "Add(items, button);",
        'SetProperty(button, "Name", spec.Text);',
        'SetProperty(button, "Text", spec.Text);',
        'SetProperty(button, "CommandParameter", spec.Command);',
        'SetProperty(button, "CommandHandler", new CommandHandler());',
        "private static object? FindById(object collection, string id)",
        "Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + \" \", true, false, false);",
    ):
        if token not in text:
            errors.append("Quick Workflow Ribbon lost its deterministic/idempotent reconciliation contract: " + token)

    for forbidden in (
        'PanelSourceId = "QS3D_AUTHOR_PANEL_SOURCE"',
        "if (source == null) source = candidate;",
        "if (CollectionContainsId(items, spec.Id)) continue;",
        "private static bool CollectionContainsId(object collection, string id)",
    ):
        if forbidden in text:
            errors.append("Quick Workflow Ribbon must not use stale flat-panel/create-only routing: " + forbidden)

    loop = text.find("foreach (var spec in Buttons)")
    find_button = text.find("var button = FindById(items, spec.Id);", loop)
    create_button = text.find("if (button == null)", find_button)
    reconcile_name = text.find('SetProperty(button, "Name", spec.Text);', create_button)
    reconcile_command = text.find('SetProperty(button, "CommandParameter", spec.Command);', reconcile_name)
    if min(loop, find_button, create_button, reconcile_name, reconcile_command) < 0 or not (
        loop < find_button < create_button < reconcile_name < reconcile_command
    ):
        errors.append("Quick Workflow must find-or-create stable buttons before reconciling current presentation/command state")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DCREATESIMILAR",
        "QS3DCREATESIMILARADV",
        "GeneratedHandleOwnershipPolicy",
        "ExistingProjectMutationContext.Require",
        "ProjectFamilyActivationService.SetActive",
        "QS3DDRAWACTIVE",
        "QS3DDRAWACTIVEADV",
        "QS3D_AUTHOR_CREATE_SIMILAR",
        "QS3D_AUTHOR_QUICK_PANEL_SOURCE",
        "Tác vụ nhanh",
        "Vẽ Tương Tự",
        "LOCAL-008",
        "intentional user selection state",
    ):
        if token not in text:
            errors.append("Create Similar documentation missing: " + token)

if errors:
    print("Create Similar preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Create Similar preflight PASS: selection/ownership/Family freshness stays guarded, authoring delegates to Active Family Quick/Advanced, and Quick Workflow uses one deterministic dedicated panel whose stable buttons reconcile current state after reinitialize.")
