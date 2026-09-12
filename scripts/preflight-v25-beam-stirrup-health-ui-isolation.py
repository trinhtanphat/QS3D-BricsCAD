from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void BeamStirrupHealth()")
end = text.find("private static List<ProjectElement> ResolveBeamTargets", start)
if start < 0 or end <= start:
    raise SystemExit("Beam Stirrup Health command structure changed")
health = text[start:end]

for token in [
    "TrySetPaletteStatusForDocument(document, message);",
    'document.Editor.WriteMessage("\\nQS3D " + message);',
    "foreach (var issue in issues.Take(50))",
]:
    if token not in health:
        raise SystemExit(f"Beam Stirrup Health UI-isolation contract missing: {token}")
if "PaletteCoordinator.SetStatus(message);" in health:
    raise SystemExit("Beam Stirrup Health must not publish process-wide palette status directly")

helper_start = text.find("private static void TrySetPaletteStatusForDocument(Document document, string message)")
report_start = text.find("private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)", helper_start)
if helper_start < 0 or report_start <= helper_start:
    raise SystemExit("Beam Stirrup Health document-aware palette isolation helper missing")
helper = text[helper_start:report_start]
for token in [
    "try",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "PaletteCoordinator.SetStatus(message);",
    "catch { }",
]:
    if token not in helper:
        raise SystemExit(f"Beam Stirrup Health palette helper missing fail-soft source-document fence: {token}")

for forbidden in ("DocumentLock", "StartTransaction", "Commit(", "Abort("):
    if forbidden in helper:
        raise SystemExit(f"Beam Stirrup Health palette helper must remain presentation-only: {forbidden}")

print("PASS: Beam Stirrup Health palette failures remain isolated behind exact active-document affinity")
