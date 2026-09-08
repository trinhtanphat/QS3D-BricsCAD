from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

helper_start = text.find("private static void TrySetPaletteStatus")
report_start = text.find("private static void Report", helper_start)
write_start = text.find("private static void TryWriteMessage", report_start)
if min(helper_start, report_start, write_start) < 0:
    print("ERROR: cannot locate Beam Stirrup UI publication helpers")
    sys.exit(1)

helper = text[helper_start:report_start]
report = text[report_start:write_start]

required_helper = [
    "private static void TrySetPaletteStatus(Document document, string message)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "PaletteCoordinator.SetStatus(message);",
]
missing = [needle for needle in required_helper if needle not in helper]
if missing:
    print("ERROR: Beam Stirrup process-wide palette publication must be fenced to the exact source active document")
    for needle in missing:
        print("  missing:", needle)
    sys.exit(1)

affinity = helper.find("ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)")
publish = helper.find("PaletteCoordinator.SetStatus(message);")
if affinity < 0 or publish < 0 or affinity > publish:
    print("ERROR: Beam Stirrup document-affinity check must run before PaletteCoordinator.SetStatus")
    sys.exit(1)

if "TrySetPaletteStatus(document, message);" not in report:
    print("ERROR: Beam Stirrup Report must route palette status through the source-document affinity helper")
    sys.exit(1)

health_start = text.find("public void BeamStirrupHealth()")
resolve_start = text.find("private static List<ProjectElement> ResolveBeamTargets", health_start)
health = text[health_start:resolve_start] if health_start >= 0 and resolve_start >= 0 else ""
if "TrySetPaletteStatus(document, message);" not in health:
    print("ERROR: Beam Stirrup Health must publish status through the source-document affinity helper")
    sys.exit(1)

finalize_start = text.find("private static void FinalizeUi(Document document, string message)")
helper_boundary = text.find("private static void TrySetPaletteStatus", finalize_start)
finalize = text[finalize_start:helper_boundary] if finalize_start >= 0 and helper_boundary >= 0 else ""
if "TrySetPaletteStatus(document, message);" not in finalize:
    print("ERROR: Beam Stirrup post-commit UI finalization must use the source-document affinity helper")
    sys.exit(1)
if "PaletteCoordinator.SetStatus(message);" in finalize:
    print("ERROR: Beam Stirrup FinalizeUi must not bypass document-affinity status publication")
    sys.exit(1)

forbidden_helper = [
    "ProjectContextCoordinator",
    "ExistingProjectMutationContext",
    "DocumentLock",
    "StartTransaction",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
]
for needle in forbidden_helper:
    if needle in helper:
        print("ERROR: Beam Stirrup status helper must remain presentation-only; found", needle)
        sys.exit(1)

print("PASS: Beam Stirrup Workspace status publication is fenced to the exact source active document")
