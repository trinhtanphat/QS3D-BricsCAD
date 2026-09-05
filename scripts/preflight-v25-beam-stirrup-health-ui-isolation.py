from pathlib import Path

SOURCE = Path("src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void BeamStirrupHealth()")
end = text.find("private static List<ProjectElement> ResolveBeamTargets", start)
if start < 0 or end <= start:
    raise SystemExit("Beam Stirrup Health command structure changed")
health = text[start:end]

required = [
    "TrySetPaletteStatus(message);",
    'document.Editor.WriteMessage("\\nQS3D " + message);',
    'foreach (var issue in issues.Take(50))',
]
for token in required:
    if token not in health:
        raise SystemExit(f"Beam Stirrup Health UI-isolation contract missing: {token}")

if "PaletteCoordinator.SetStatus(message);" in health:
    raise SystemExit("Beam Stirrup Health still allows palette publication to abort editor diagnostics")

helper_start = text.find("private static void TrySetPaletteStatus(")
report_start = text.find("private static void Report(", helper_start)
if helper_start < 0 or report_start <= helper_start:
    raise SystemExit("Beam Stirrup Health palette isolation helper missing")
helper = text[helper_start:report_start]
if "try { PaletteCoordinator.SetStatus(message); }" not in helper or "catch { }" not in helper:
    raise SystemExit("Beam Stirrup Health palette publication must be exception-isolated")

print("PASS: Beam Stirrup Health palette failures cannot suppress already-computed editor diagnostics")
