from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
package = (ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageUx.cs").read_text(encoding="utf-8")
suite = (ROOT / "tests/QS3D.Core.SmokeTests/BenchmarkParitySuiteSmoke.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/TakeoffPackageEvidenceSourceAffinitySmoke.cs").read_text(encoding="utf-8")

required = {
    "sheet identity lookup": (package, "sheetsById.TryGetValue(item.SheetId, out sourceSheet)"),
    "exact source-reference fence": (package, "string.Equals(item.SourceReference, sourceSheet.SourceReference, StringComparison.Ordinal)"),
    "stable validation code": (package, '"PKG.STALE_EVIDENCE_SOURCE"'),
    "registered smoke": (suite, "TakeoffPackageEvidenceSourceAffinitySmoke.Run();"),
    "replacement-source regression": (smoke, '"A501-old.pdf"'),
    "admitted-source regression": (smoke, '"A501-new.pdf"'),
    "inventory publication block": (smoke, "result.CanEstimate || result.Inventory.Count != 0"),
}

missing = [name for name, (text, needle) in required.items() if needle not in text]
if missing:
    for item in missing:
        print(f"ERROR: {item}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: Takeoff package drawing evidence is fenced to the exact admitted sheet source.")
