from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Mep" / "MepTbqProjection.cs"
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "MepTbqCommands.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"FAIL: {label}: forbidden {token!r}")


core = CORE.read_text(encoding="utf-8")
host = HOST.read_text(encoding="utf-8")

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

print("PASS: host MEP takeoff is wired into canonical TBQ/BQ projection and deterministic report/export contracts without invented pricing or bbox quantity approximation.")
