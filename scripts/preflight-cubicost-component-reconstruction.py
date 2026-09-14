from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsCubicostComponentReconstruction.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsCubicostComponentReconstructionSmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class CubicostComponentReconstructionVerifier",
    "sealed class CubicostVerifiedComponentReconstruction",
    "QuantifyVerified",
    "CubicostConcreteFormworkDomainOrchestrator",
    "Duplicate reconstructed component id",
    "Duplicate component review decision",
    "Reconstructed component has no review decision",
    "Review decision references an unknown reconstructed component",
    "Proposed reconstruction cannot enter quantity publication",
    "component.Evidence.Confidence <= 0d",
    "Source.Evidence",
):
    if token not in text:
        raise SystemExit(f"Cubicost component-reconstruction preflight: missing required token: {token}")

for forbidden in (
    "Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices",
    "System.Windows.Forms", "PresentationCore", "WindowsBase", "System.Windows"
):
    if forbidden in text:
        raise SystemExit(f"Cubicost component-reconstruction preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "corrected concrete volume",
    "corrected formwork area",
    "downstream evidence identity preserved",
    "missing review",
    "duplicate review",
    "orphan review",
    "proposed reconstruction",
):
    if token not in smoke_text:
        raise SystemExit(f"Cubicost component-reconstruction preflight: smoke coverage missing token: {token}")

print("Cubicost component-reconstruction verification preflight passed.")
