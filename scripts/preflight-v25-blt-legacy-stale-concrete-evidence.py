#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BLT_REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"
LIFECYCLE_REL = "src/QS3D.Core/Domain/ProjectElementQuantityLifecycleExtensions.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    blt_path = ROOT / BLT_REL
    lifecycle_path = ROOT / LIFECYCLE_REL
    if not blt_path.exists():
        fail(f"missing required source: {BLT_REL}")
    if not lifecycle_path.exists():
        fail(f"missing required source: {LIFECYCLE_REL}")

    blt_source = blt_path.read_text(encoding="utf-8")
    lifecycle_source = lifecycle_path.read_text(encoding="utf-8")

    method_start = require(blt_source, "private static void ApplyLegacyEvidence", "missing BLT legacy evidence mutation path")
    method_end = require(blt_source, "private static void WriteSummary", "missing BLT legacy evidence method boundary")
    method = blt_source[method_start:method_end]

    concrete_if = require(method, "if (candidate.LegacyConcreteM3.HasValue)", "missing exact legacy concrete admission branch")
    formwork_if = require(method, "if (candidate.LegacyFormworkM2.HasValue)", "missing formwork boundary after concrete handling")
    concrete_block = method[concrete_if:formwork_if]

    for quantity in ("GrossVolumeM3", "NetVolumeM3", "MeasuredSolidVolumeM3"):
        require(concrete_block, f'element.SetQuantity("{quantity}", concrete)', f"exact concrete must continue to publish {quantity}")
        require(concrete_block, f'element.RemoveQuantity("{quantity}")', f"missing stale exact concrete cleanup for {quantity}")

    require(concrete_block, 'element.SetProperty("CAD.BLT.LegacyConcreteM3"', "exact concrete must continue to publish canonical BLT evidence property")
    require(concrete_block, 'element.RemoveSemanticProperty("CAD.BLT.LegacyConcreteM3")', "missing stale canonical BLT concrete property cleanup")

    if 'SetQuantity("GrossVolumeM3", 0' in concrete_block or 'SetQuantity("NetVolumeM3", 0' in concrete_block or 'SetQuantity("MeasuredSolidVolumeM3", 0' in concrete_block:
        fail("absence of exact concrete evidence must remove stale evidence, not fabricate zero quantities")

    for needle, message in (
        ("public static bool RemoveSemanticProperty(this ProjectElement element, string name)", "missing shared lifecycle-aware semantic property removal surface"),
        ("element == null", "RemoveSemanticProperty must reject a null element"),
        ("string.IsNullOrWhiteSpace(name)", "RemoveSemanticProperty must reject missing property names"),
        ("name.Any(char.IsControl)", "RemoveSemanticProperty must reject control characters in property names"),
        ("XmlConvert.VerifyXmlChars(key)", "RemoveSemanticProperty must enforce XML-safe canonical property keys"),
        ("element.RemoveProperty(key)", "RemoveSemanticProperty must delegate to canonical ProjectElement property removal lifecycle"),
    ):
        require(lifecycle_source, needle, message)

    print("PASS: V25 BLT legacy re-import clears stale exact concrete evidence through lifecycle-aware removals.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
