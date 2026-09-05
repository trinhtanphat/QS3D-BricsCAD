#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE_REL = "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"
EXT_REL = "src/QS3D.Core/Domain/ProjectElementPropertyLifecycleExtensions.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    service_path = ROOT / SERVICE_REL
    extension_path = ROOT / EXT_REL
    if not service_path.exists():
        fail(f"missing required source: {SERVICE_REL}")
    if not extension_path.exists():
        fail(f"missing required source: {EXT_REL}")

    source = service_path.read_text(encoding="utf-8")
    extension = extension_path.read_text(encoding="utf-8")
    start = require(source, "private static bool CaptureSnapshotCore", "missing semantic capture core")
    end = require(source, "private static void RestoreCaptureOrThrow", "missing semantic capture core boundary")
    method = source[start:end]
    metric_start = require(source, "private static void ReplaceSourceMetric", "missing source metric helper")
    metric_end = require(source[metric_start:], "public static int GenerateRoomFinishes", "missing source metric helper boundary") + metric_start
    metric = source[metric_start:metric_end]

    for forbidden, label in (
        ('element.Properties["Layer"] = snapshot.Layer;', "Layer direct dictionary write"),
        ('element.Properties.Remove(key);', "CAD metadata direct dictionary removal"),
        ('element.Properties["CAD." + item.Key] = item.Value ?? string.Empty;', "CAD metadata direct dictionary write"),
        ('element.Properties.Remove("VolumeM3");', "VolumeM3 direct dictionary removal"),
        ('element.Properties["CAD.SolidMetricSource"] = "Solid3d.MassProperties";', "solid metric source direct dictionary write"),
        ('element.Properties.Remove("CAD.SolidMetricSource");', "solid metric source direct dictionary removal"),
    ):
        if forbidden in method:
            fail(label + " bypasses canonical ProjectElement property lifecycle")

    for forbidden, label in (
        ('element.Properties.Remove(key);', "source metric direct dictionary removal"),
        ('element.Properties[key] = value.Value.ToString', "source metric direct dictionary write"),
    ):
        if forbidden in metric:
            fail(label + " bypasses canonical ProjectElement property lifecycle")

    for required, label in (
        ('element.SetProperty("Layer", snapshot.Layer);', "Layer must use SetProperty"),
        ('element.RemovePropertyLifecycle(key);', "CAD property cleanup must use lifecycle removal"),
        ('element.SetProperty("CAD." + item.Key, item.Value ?? string.Empty);', "CAD metadata must use SetProperty"),
        ('element.RemovePropertyLifecycle("VolumeM3");', "VolumeM3 removal must use lifecycle removal"),
        ('element.SetProperty("CAD.SolidMetricSource", "Solid3d.MassProperties");', "solid metric source must use SetProperty"),
        ('element.RemovePropertyLifecycle("CAD.SolidMetricSource");', "solid metric source removal must use lifecycle removal"),
    ):
        require(method, required, label)

    require(metric, 'element.RemovePropertyLifecycle(key);', "source metric removal must use lifecycle removal")
    require(metric, 'element.SetProperty(key, value.Value.ToString("R", CultureInfo.InvariantCulture));', "source metric write must use SetProperty")

    require(extension, "public static bool RemovePropertyLifecycle(this ProjectElement element, string name)", "missing public adapter-safe property removal extension")
    require(extension, "return element.RemoveProperty(name);", "property removal extension must delegate to canonical ProjectElement removal")
    if "element.Properties.Remove" in extension:
        fail("property removal extension must not reimplement raw dictionary removal")

    print("PASS: V25 semantic capture routes persisted host properties through canonical ProjectElement lifecycle APIs.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
