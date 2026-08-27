from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspaceFloatingToolHost.cs"

text = SOURCE.read_text(encoding="utf-8")

required = [
    'StringComparer.Ordinal.Equals(contentKey, contentKey.Trim())',
    'throw new ArgumentException("Floating tool key must not contain leading or trailing whitespace."',
    '_tools.TryGetValue(contentKey, out var hosted)',
    'RememberBounds(contentKey, hosted.Window)',
    '_tools.Remove(contentKey)',
]

for marker in required:
    if marker not in text:
        raise SystemExit(f"missing floating-close canonicality marker: {marker}")

for forbidden in [
    '_tools.TryGetValue(contentKey.Trim()',
    'RememberBounds(contentKey.Trim()',
    '_tools.Remove(contentKey.Trim()',
]:
    if forbidden in text:
        raise SystemExit(f"floating close must not normalize identity via Trim(): {forbidden}")

print("PASS workspace floating close canonicality")
