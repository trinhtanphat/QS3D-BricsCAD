#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BLT_REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"
REMOVE_REL = "src/QS3D.Core/Domain/ProjectElementQuantityLifecycleExtensions.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    blt_path = ROOT / BLT_REL
    remove_path = ROOT / REMOVE_REL
    if not blt_path.exists():
        fail(f"missing required source: {BLT_REL}")
    if not remove_path.exists():
        fail(f"missing required source: {REMOVE_REL}")

    blt_source = blt_path.read_text(encoding="utf-8")
    remove_source = remove_path.read_text(encoding="utf-8")

    method_start = require(blt_source, "private static void ApplyLegacyEvidence", "missing BLT legacy evidence mutation path")
    method_end = require(blt_source, "private static void WriteSummary", "missing BLT legacy evidence method boundary")
    method = blt_source[method_start:method_end]

    for key in (
        "CAD.BLT.SourceSystem",
        "CAD.BLT.EvidenceMode",
        "CAD.BLT.CategoryEvidence",
        "CAD.BLT.LegacyConcreteM3",
        "CAD.BLT.LegacyFormworkM2",
        "CAD.BLT.FormworkStatus",
        "Name",
        "Material",
        "CAD.BLT.UnresolvedFloorHint",
        "CAD.BLT.UnresolvedFamilyHint",
    ):
        require(method, f'element.SetProperty("{key}"', f"BLT evidence key {key} must use ProjectElement.SetProperty")

    if "element.Properties[" in method:
        fail("BLT legacy evidence mutation must not bypass ProjectElement.SetProperty with direct Properties writes")
    if "element.Quantities.Remove(" in method:
        fail("BLT legacy evidence mutation must not bypass quantity lifecycle with direct dictionary removal")

    remove_pos = require(method, 'element.RemoveQuantity("FormworkM2")', "missing lifecycle-aware stale Formwork quantity removal")
    pending_pos = require(method, 'element.SetProperty("CAD.BLT.FormworkStatus", "PENDING_EXACT_EVIDENCE")', "missing pending-formwork evidence status")
    if remove_pos > pending_pos:
        fail("stale exact Formwork quantity must be removed before publishing pending evidence status")

    for needle, message in (
        ("public static bool RemoveQuantity(this ProjectElement element, string name)", "missing shared ProjectElement quantity-removal extension"),
        ("element == null", "RemoveQuantity must reject a null element"),
        ("string.IsNullOrWhiteSpace(name)", "RemoveQuantity must reject missing quantity names"),
        ("name.Any(char.IsControl)", "RemoveQuantity must reject control characters in quantity names"),
        ("XmlConvert.VerifyXmlChars(key)", "RemoveQuantity must enforce XML-safe canonical quantity keys"),
        ("element.Quantities.Remove(key)", "RemoveQuantity must remove only the canonical quantity key"),
        ("element.MarkDirty(ElementDirtyFlags.Quantity)", "RemoveQuantity must mark element quantity lifecycle dirty when removal occurs"),
        ("return false", "RemoveQuantity must preserve no-op semantics when the quantity is absent"),
        ("return true", "RemoveQuantity must report a successful lifecycle mutation"),
    ):
        require(remove_source, needle, message)

    remove_impl_pos = require(remove_source, "element.Quantities.Remove(key)", "missing quantity removal")
    dirty_pos = require(remove_source, "element.MarkDirty(ElementDirtyFlags.Quantity)", "missing quantity dirty lifecycle")
    if not remove_impl_pos < dirty_pos:
        fail("quantity lifecycle must mark dirty only after a successful removal")

    print("PASS: BLT legacy evidence mutations use canonical property lifecycle plus bounded Core quantity-removal lifecycle without direct persisted-dictionary bypass.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
