#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticScheduleNativeTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/SemanticScheduleNativeTableCommands.cs"
HEALTH = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
HUB = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml"
DOCS = ROOT / "docs/SEMANTIC-SCHEDULES.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


builder = read(BUILDER)
commands = read(COMMANDS)
health = read(HEALTH)
release = read(RELEASE)
hub = read(HUB)
docs = read(DOCS)

for token in (
    'MetadataPrefix = "QS3D.Documentation.NativeSemanticScheduleTable."',
    'RegAppName = "QS3DDOC"',
    'DocumentId = "SemanticCustomSchedule"',
    'SemanticScheduleCatalog.Build(project, currentDefinition)',
    'table.SetSize(semanticTable.Rows.Count + 2, semanticTable.Headers.Count)',
    'ProjectStateSnapshot.Capture(project)',
    'HasMatchingOwnership(table, project.ProjectId, scheduleId, fingerprint)',
    'PersistedHandles(ProjectState project)',
    'new ModelHealthIssue(code, severity, prefix + message, string.Empty)',
):
    if token not in builder:
        errors.append("custom schedule builder missing contract token: " + token)

if "semanticTable.Rows.Count == 0" in builder:
    errors.append("valid zero-match custom schedules must remain renderable as header-only native Tables")
if "table.Erase();" not in builder or builder.find("HasMatchingOwnership(table, project.ProjectId, scheduleId, fingerprint)") > builder.find("table.Erase();"):
    errors.append("native replacement/removal must verify exact project/schedule/fingerprint ownership before erase")

for command in (
    "QS3DSCHEDULETABLE",
    "QS3DSCHEDULETABLEREFRESH",
    "QS3DSCHEDULETABLEHEALTH",
    "QS3DSCHEDULETABLEREMOVE",
):
    if '[CommandMethod("' + command + '"' not in commands:
        errors.append("missing native custom schedule command: " + command)

for token in (
    "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
    "ExistingProjectMutationContext.Require(document, operation)",
    "expectedProjectId = previewProject.ProjectId",
    "expectedChangeVersion = previewProject.ChangeVersion",
    "project.ChangeVersion != expectedChangeVersion",
    "PromptDefinition(document, previewProject)",
    "PromptRemovableScheduleId(document, previewProject)",
    "RequireSupportedUcs(document)",
):
    if token not in commands:
        errors.append("custom schedule command lifecycle token missing: " + token)
if "ProjectContextCoordinator.GetOrCreate" in commands:
    errors.append("custom schedule native commands must never bootstrap/cache project state")

build_start = commands.find('[CommandMethod("QS3DSCHEDULETABLE"')
refresh_start = commands.find('[CommandMethod("QS3DSCHEDULETABLEREFRESH"')
if build_start >= 0 and refresh_start > build_start:
    build = commands[build_start:refresh_start]
    prompt = build.find("PromptDefinition(document, previewProject)")
    point = build.find("document.Editor.GetPoint")
    bind = build.find('RequireExistingProject(document, "Custom Schedule Table")')
    if min(prompt, point, bind) < 0 or not (prompt < point < bind):
        errors.append("build must finish schedule/point prompting before canonical project mutation bind")

for token in (
    "SemanticScheduleNativeTableBuilder.Inspect(document, project)",
    'normalized.StartsWith("CUSTOM_SCHEDULE_TABLE_", StringComparison.Ordinal)',
    "SemanticScheduleNativeTableBuilder.PersistedHandles(project)",
):
    if token not in health:
        errors.append("Health All missing custom schedule artifact integration: " + token)

for token in (
    "SemanticScheduleNativeTableBuilder.Inspect(document, project)",
    'StartsWith("CUSTOM_SCHEDULE_TABLE_", StringComparison.OrdinalIgnoreCase)',
    "SemanticScheduleNativeTableBuilder.PersistedHandles(currentProject)",
):
    if token not in release:
        errors.append("Release Check missing custom schedule artifact integration: " + token)

for command in (
    "QS3DSCHEDULETABLE",
    "QS3DSCHEDULETABLEREFRESH",
    "QS3DSCHEDULETABLEHEALTH",
    "QS3DSCHEDULETABLEREMOVE",
):
    if 'Tag="' + command + '"' not in hub:
        errors.append("Schedule Hub missing command tag: " + command)

for token in (
    "does not calculate BQ",
    "does not calculate BBS",
    "header-only",
    "ProjectId",
    "ChangeVersion",
    "LOCAL_ONLY",
):
    if token not in docs:
        errors.append("semantic schedule docs missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: native custom semantic schedules preserve header-only rendering, prompt-before-bind freshness, per-schedule ownership-safe Table lifecycle, Hub wiring, and Health/Release artifact diagnostics without becoming a BQ/BBS engine.")
