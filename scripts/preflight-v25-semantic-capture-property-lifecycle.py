#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REL = "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    path = ROOT / REL
    if not path.exists():
        fail(f"missing required source: {REL}")
    source = path.read_text(encoding="utf-8")
    start = require(source, "private static bool CaptureSnapshotCore", "missing semantic capture core")
    end = require(source, "private static void RestoreCaptureOrThrow", "missing semantic capture core boundary")
    method = source[start:end]

    for forbidden, label in (
        ('element.Properties["Layer"] = snapshot.Layer;', "Layer direct dictionary write"),
        ('element.Properties["CAD." + item.Key] = item.Value ?? string.Empty;', "CAD metadata direct dictionary write"),
        ('element.Properties.Remove("VolumeM3");', "VolumeM3 direct dictionary removal"),
        ('element.Properties["CAD.SolidMetricSource"] = "Solid3d.MassProperties";', "solid metric source direct dictionary write"),
        ('element.Properties.Remove("CAD.SolidMetricSource");', "solid metric source direct dictionary removal"),
    ):
        if forbidden in method:
            fail(label + " bypasses canonical ProjectElement property lifecycle")

    for required, label in (
        ('element.SetProperty("Layer", snapshot.Layer);', "Layer must use SetProperty"),
        ('element.SetProperty("CAD." + item.Key, item.Value ?? string.Empty);', "CAD metadata must use SetProperty"),
        ('element.RemoveProperty("VolumeM3");', "VolumeM3 removal must use RemoveProperty"),
        ('element.SetProperty("CAD.SolidMetricSource", "Solid3d.MassProperties");', "solid metric source must use SetProperty"),
        ('element.RemoveProperty("CAD.SolidMetricSource");', "solid metric source removal must use RemoveProperty"),
    ):
        require(method, required, label)

    print("PASS: V25 semantic capture routes persisted host properties through canonical ProjectElement lifecycle APIs.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
