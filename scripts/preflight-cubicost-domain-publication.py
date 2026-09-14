from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsCubicostDomainPublication.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsCubicostDomainPublicationSmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class CubicostConcreteFormworkPublicationWorkflow",
    "CubicostConcreteDomainWorkflow",
    "CubicostFormworkDomainWorkflow",
    "CubicostReviewedQuantityDownstreamBridge",
    "BuildInventory",
    "BuildCommercialHandoffs",
    "Concrete/Formwork domain publication cardinality mismatch",
    "evidence generation mismatch",
    '"m3"',
    '"m2"',
):
    if token not in text:
        raise SystemExit(f"Cubicost domain-publication preflight: missing required token: {token}")

for forbidden in (
    "Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices",
    "System.Windows.Forms", "PresentationCore", "WindowsBase", "System.Windows"
):
    if forbidden in text:
        raise SystemExit(f"Cubicost domain-publication preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "three destinations per domain line",
    "evidence identity preserved",
    "evidence generation mismatch",
    "missing formwork peer",
):
    if token not in smoke_text:
        raise SystemExit(f"Cubicost domain-publication preflight: smoke coverage missing token: {token}")

print("Cubicost Concrete/Formwork domain-publication preflight passed.")
