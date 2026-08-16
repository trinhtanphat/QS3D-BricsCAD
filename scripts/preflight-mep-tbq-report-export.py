from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Mep" / "MepTbqProjection.cs"
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "MepTbqCommands.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "MepTbqProjectionSmoke.cs"
MEP_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "MepRecognitionSmoke.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"FAIL: {label}: forbidden {token!r}")


core = CORE.read_text(encoding="utf-8")
host = HOST.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
mep_smoke = MEP_SMOKE.read_text(encoding="utf-8")

require(core, 'OwnedItemPrefix = "QS3D.MEP."', "canonical MEP-owned BQ namespace")
require(core, "SHA256.Create()", "deterministic MEP report identity")
require(core, "SerializeCsv", "deterministic export DTO/CSV contract")
require(core, 'new TbqBillItem(itemCode, description, unit, "MEP", quantity, 0m)', "no invented MEP pricing")
require(core, "current.BuildUpRates", "preserve TBQ build-up state")
require(core, "current.RateReferences.Edges", "preserve TBQ rate references")
require(core, "current.Library.Entries", "preserve BQ library state")

require(host, 'CommandMethod("QS3DMEPTBQIMPORT"', "native MEP-to-TBQ import command")
require(host, 'CommandMethod("QS3DMEPTBQREPORT"', "native deterministic report command")
require(host, "MepRecognitionProfileProvider.Current", "reuse configurable recognition profile")
require(host, "MepQuantityService().Aggregate", "reuse canonical MEP quantity aggregation")
require(host, "ProjectTbqWorkspace.Open", "canonical project-bound TBQ state")
require(host, "ProjectContextCoordinator.RequireBackingStoreUnchanged", "freshness guard")
require(host, "ProjectStateSnapshot.Capture", "rollback snapshot")
require(host, "ProjectContextCoordinator.Save(document)", "canonical project save")
require(host, "snapshot.Restore(project)", "rollback on save failure")
require(host, "SurfaceAreaDrawingUnitsSquared ?? snapshot.AreaDrawingUnitsSquared", "host property fallback")
forbid(host, "GeometricExtents", "no bounding-box quantity approximation")
forbid(host, "ClashDetectionService", "keep clash/review lane isolated")

require(smoke, "ProjectsCanonicalMetricsAndPreservesWorkspace", "executable MEP/TBQ projection semantics smoke")
require(smoke, 'Equal(5m, ductCount.Quantity, "COUNT uses QuantityCount, not ElementCount")', "COUNT quantity regression coverage")
require(smoke, "StableProjectionAndCsv", "stable projection identity and CSV smoke")
require(smoke, "EmptyMetricsDoNotCreateRows", "zero-metric suppression smoke")
require(smoke, "UnrepresentableMetricFailsClosed", "numeric fail-closed smoke")
require(smoke, 'Equal(0m, item.UnitRate, "projected MEP rate remains zero")', "no invented price executable coverage")
require(mep_smoke, "MepTbqProjectionSmoke.Run();", "MEP/TBQ projection smoke registration")

print("PASS: host MEP takeoff is wired into canonical TBQ/BQ projection and deterministic report/export contracts with executable Core smoke coverage, no invented pricing, and no bbox quantity approximation.")
