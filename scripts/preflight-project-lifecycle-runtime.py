#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing lifecycle source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


lifecycle = read(ADAPTER / "DocumentLifecycleCoordinator.cs")
workspace = read(ADAPTER / "UI" / "WorkspacePanel.xaml.cs")
context = read(ADAPTER / "ProjectContextCoordinator.cs")
probe = read(ADAPTER / "ProjectLifecycleProbeCommands.cs")
runner = read(ROOT / "scripts" / "test-bricscad-v25-project-lifecycle.ps1")

ensure_start = lifecycle.find("private static void EnsureProject(")
ensure_body = lifecycle[ensure_start:] if ensure_start >= 0 else ""
require(ensure_body, "ProjectContextCoordinator.TryGetReadOnly(document, out _)", "document activation")
require(ensure_body, "PaletteCoordinator.ResetForUnavailableProject(", "absent-project palette reset")
if "ProjectContextCoordinator.GetOrCreate(document)" in ensure_body:
    errors.append("document create/activate must not create or cache a replacement project")

refresh_start = workspace.find("public void RefreshProject()")
refresh_end = workspace.find("public void SetStatus", refresh_start)
refresh = workspace[refresh_start:refresh_end] if refresh_start >= 0 and refresh_end > refresh_start else ""
require(refresh, "ExistingProjectMutationContext.TryGet(doc, out var project)", "mutation-capable Workspace canonical project")
if "ProjectContextCoordinator.TryGetReadOnly" in refresh or "ProjectContextCoordinator.GetOrCreate" in refresh:
    errors.append("Workspace mutation view-model must bind an existing canonical ProjectState without cold-creating one")

for token in (
    "EnsureUsable(existing);",
    "project = LoadExistingProjectOrThrow(path);",
    "No replacement project was created and the sidecar was left unchanged.",
    "private static void EnsureUsable(ProjectState project)",
):
    require(context, token, "corrupt-sidecar fail-closed loader")
get_start = context.find("public static ProjectState GetOrCreate")
read_start = context.find("public static bool TryGetReadOnly", get_start)
get_or_create = context[get_start:read_start] if get_start >= 0 and read_start > get_start else ""
for forbidden in ("QS3D.LoadWarning", "QS3D.FailedProjectPath", "catch (Exception ex)"):
    if forbidden in get_or_create:
        errors.append("GetOrCreate must not cache a default recovery replacement after load failure: " + forbidden)

for token in (
    '[CommandMethod("QS3DLIFECYCLESEED"',
    '[CommandMethod("QS3DLIFECYCLEAFTERSAVE"',
    '[CommandMethod("QS3DLIFECYCLEPROBE"',
    "if (SkipOutsideAutomation(resultPath)) return;",
    "ProjectContextCoordinator.HasPendingChanges(document)",
    "ExistingProjectMutationContext.TryGet(documentA, out var canonicalA)",
    "ExistingProjectMutationContext.TryGet(documentB, out var canonicalB)",
    "ProjectContextCoordinator.Forget(documentA);",
    "DetachedProjectStamp.Capture(observedA)",
    "detachedA.EnsureUnchanged(observedA);",
    "EnsureCorruptSidecarFailsClosed(documentD);",
    "File.Replace(tempPath, path, backupPath, true);",
    '"nonce=" + nonce',
    '"absent_sidecar_noncreating=true"',
    '"corrupt_sidecar_fail_closed=true"',
):
    require(probe, token, "lifecycle automation probe")
for forbidden in (
    '"project_id="', '"drawing_path="', '"drawing_fingerprint="',
    '"handle="', '"error_message="', "ex.Message"
):
    if forbidden in probe:
        errors.append("lifecycle marker/privacy contract contains forbidden raw evidence token: " + forbidden)

for token in (
    "[switch]$ConfirmSyntheticFixture",
    '"QS3D-Sample.dwg"',
    '"generated"',
    "git -C $repoRoot status --porcelain",
    '"project-lifecycle-a.reference-copy.dwg"',
    '"project-lifecycle-b.reference-copy.dwg"',
    '"project-lifecycle-c.reference-copy.dwg"',
    '"project-lifecycle-d.reference-copy.dwg"',
    '"<not-a-qs3d-project />"',
    '"QS3DLIFECYCLESEED", "_.QSAVE", "QS3DLIFECYCLEAFTERSAVE"',
    '"_.OPEN", (\'"\' + $drawingB + \'"\')',
    '"_.OPEN", (\'"\' + $drawingC + \'"\')',
    '"_.OPEN", (\'"\' + $drawingD + \'"\')',
    '"QS3DLIFECYCLEPROBE"',
    '"corrupt_sidecar_fail_closed"',
    "$corruptHashBefore",
    "$corruptHashAfter",
    "Restore-EnvironmentValue -Name $name",
    "Stop-Qs3dLaunchedProcess -Process $process",
):
    require(runner, token, "local lifecycle runner")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: passive document activation is non-creating, corrupt sidecars fail closed, Workspace keeps canonical mutable state, and the exact-SHA synthetic V25 runner proves SaveComplete/reopen/multi-DWG isolation without raw project evidence.")
