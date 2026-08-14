#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
UPDATER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Updates" / "UpdateBootstrapper.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeDiagnosticsCommands.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(label + ": missing " + repr(needle))


def require_order(text, first, second, label, failures):
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        failures.append(label + ": expected order " + repr(first) + " before " + repr(second))


def main():
    plugin = PLUGIN.read_text(encoding="utf-8")
    updater = UPDATER.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")
    failures = []

    require(plugin, "catch\n            {\n                TeardownHostServices();\n                throw;", "plugin startup rollback", failures)
    require(plugin, "catch (Exception ex)\n            {\n                ReportOptionalStartupFailure(\"Update service\", ex);", "optional updater containment", failures)
    require(plugin, "public void Terminate()\n        {\n            TeardownHostServices();\n        }", "single contained teardown entry", failures)
    for cleanup in (
        "UpdateBootstrapper.Stop",
        "RibbonInitializationCoordinator.Stop",
        "DocumentLifecycleCoordinator.Stop",
        "PaletteCoordinator.Dispose",
        "UpdateRibbonAugmenter.Reset",
        "QuantityReferenceRibbonAugmenter.Reset",
        "QuickWorkflowRibbonAugmenter.Reset",
        "ReferenceWallRibbonAugmenter.Reset",
        "ProjectRibbonAugmenter.Reset",
        "RibbonBootstrapper.Reset",
    ):
        require(plugin, "TryCleanup(" + cleanup + ");", "independent teardown " + cleanup, failures)
    require(plugin, "private static void TryCleanup(Action cleanup)", "cleanup containment helper", failures)

    require(updater, "var subscribed = false;", "updater subscription rollback state", failures)
    require_order(updater, "UpdateCoordinator.Instance.Start();", "_started = true;", "updater only marks started after coordinator start", failures)
    require(updater, "UpdateCoordinator.Instance.AutomaticUpdateFound -= OnAutomaticUpdateFound;", "updater event rollback/unsubscribe", failures)
    require(updater, "try { UpdateCoordinator.Instance.Stop(); }\n                catch { }\n                _started = false;\n                throw;", "updater failed-start rollback", failures)
    stop_start = updater.find("internal static void Stop()")
    stop_end = updater.find("private static void TryScheduleVerifiedUpdateOnExit()", stop_start + 1)
    stop_body = updater[stop_start:stop_end] if stop_start >= 0 and stop_end > stop_start else ""
    if not stop_body:
        failures.append("updater teardown idempotence: UpdateBootstrapper.Stop body not found")
    else:
        require_order(stop_body, "_started = false;\n            TryScheduleVerifiedUpdateOnExit();", "try { UpdateCoordinator.Instance.AutomaticUpdateFound -= OnAutomaticUpdateFound; }", "updater teardown idempotence", failures)
    require(updater, "try { UpdateCenterWindowHost.Close(); }\n            catch { }", "updater window teardown containment", failures)

    require(runtime, "#if !BRICSCAD_V26\nusing Teigha.BoundaryRepresentation;\n#endif", "V25-only BREP compile guard", failures)
    require(runtime, "var expectedRuntime = NativeRuntimeAssembliesMatch(brxAssembly, tdAssembly);", "complete native runtime verdict", failures)
    require(runtime, "return Major(typeof(Brep).Assembly) == ExpectedRuntimeMajor;", "BREP runtime-major check", failures)
    require(runtime, "return VersionText(typeof(Brep).Assembly);", "BREP version diagnostics", failures)
    require(runtime, "TD_MgdBrep version:", "BREP runtime output", failures)
    require(runtime, "complete native dependency set", "runtime PASS contract", failures)
    require(v26_project, "<DefineConstants>$(DefineConstants);BRICSCAD_V26</DefineConstants>", "V26 shared-source compile symbol", failures)
    if "<Reference Include=\"TD_MgdBrep\"" in v26_project:
        failures.append("V26 project unexpectedly acquired a V25-only TD_MgdBrep compile reference")

    if failures:
        print("V25 host lifecycle/native readiness preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 host lifecycle teardown is contained and native runtime readiness includes guarded BREP identity.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
