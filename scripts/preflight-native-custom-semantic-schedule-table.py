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
    'PersistedTokenCandidates(project)',
    'if (!IsToken(token))',
    '"Persisted custom schedule Table metadata contains a malformed owner token."',
    'Token = token;',
    'public string Token { get; }',
    '!string.Equals(keys.Token, Token(storedScheduleId), StringComparison.Ordinal)',
):
    if token not in builder:
        errors.append("custom schedule builder missing contract token: " + token)

if "semanticTable.Rows.Count == 0" in builder:
    errors.append("valid zero-match custom schedules must remain renderable as header-only native Tables")
if "table.Erase();" not in builder or builder.find("HasMatchingOwnership(table, project.ProjectId, scheduleId, fingerprint)") > builder.find("table.Erase();"):
    errors.append("native replacement/removal must verify exact project/schedule/fingerprint ownership before erase")

validate_start = builder.find("private static void ValidatePersistedState")
validate_end = builder.find("private static void ErasePrevious", validate_start + 1)
validate = builder[validate_start:validate_end] if validate_start >= 0 and validate_end > validate_start else ""
if "keys.Token" not in validate or "Token(storedScheduleId)" not in validate:
    errors.append("persisted custom schedule metadata must bind the actual owner bucket token to the stored ScheduleId hash")
if "Token(storedScheduleId), Token(scheduleId)" in validate:
    errors.append("persisted custom schedule validation must not self-compare hashes derived only from schedule IDs while ignoring the actual metadata bucket token")

inspect_start = builder.find("public static IReadOnlyList<ModelHealthIssue> Inspect")
inspect_end = builder.find("private static void InspectToken", inspect_start + 1)
inspect = builder[inspect_start:inspect_end] if inspect_start >= 0 and inspect_end > inspect_start else ""
if "foreach (var token in PersistedTokenCandidates(project))" not in inspect:
    errors.append("custom schedule Health must inspect every metadata owner-token candidate, including malformed tokens")
if "foreach (var token in PersistedTokens(project))" in inspect:
    errors.append("custom schedule Health must not silently filter malformed metadata owner tokens before diagnostics")
if "CUSTOM_SCHEDULE_TABLE_METADATA_INVALID" not in inspect or "if (!IsToken(token))" not in inspect:
    errors.append("custom schedule Health must report malformed owner tokens as metadata-invalid")

persisted_start = builder.find("private static IReadOnlyList<string> PersistedTokens")
persisted_end = builder.find("private static bool IsToken", persisted_start + 1)
persisted = builder[persisted_start:persisted_end] if persisted_start >= 0 and persisted_end > persisted_start else ""
for token in ("PersistedTokenCandidates(project)", ".Where(IsToken)"):
    if token not in persisted:
        errors.append("canonical persisted schedule/handle enumeration must retain valid-token filtering: " + token)

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

print("PASS: native custom semantic schedules preserve header-only rendering, prompt-before-bind freshness, bind ScheduleId to the actual owner bucket token, fail closed on malformed/partial owner metadata, retain canonical-token selection safety, keep per-schedule ownership-safe Table lifecycle, Hub wiring, and Health/Release diagnostics without becoming a BQ/BBS engine.")
