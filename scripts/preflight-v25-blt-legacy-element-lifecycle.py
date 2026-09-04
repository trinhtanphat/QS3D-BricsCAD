#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BLT_REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"
ELEMENT_REL = "src/QS3D.Core/Domain/ProjectElement.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    blt_path = ROOT / BLT_REL
    element_path = ROOT / ELEMENT_REL
    if not blt_path.exists():
        fail(f"missing required source: {BLT_REL}")
    if not element_path.exists():
        fail(f"missing required source: {ELEMENT_REL}")

    blt_source = blt_path.read_text(encoding="utf-8")
    element_source = element_path.read_text(encoding="utf-8")

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

    set_quantity_start = require(element_source, "public void SetQuantity", "missing canonical quantity setter")
    remove_quantity_start = require(element_source, "public bool RemoveQuantity", "missing symmetric quantity removal lifecycle API")
    stale_start = require(element_source, "public void MarkGeneratedGeometryStale", "missing ProjectElement quantity-method boundary")
    if not (set_quantity_start < remove_quantity_start < stale_start):
        fail("RemoveQuantity must remain adjacent to canonical quantity mutation lifecycle")

    remove_quantity = element_source[remove_quantity_start:stale_start]
    for needle, message in (
        ("string.IsNullOrWhiteSpace(name)", "RemoveQuantity must reject missing quantity names"),
        ("name.Any(char.IsControl)", "RemoveQuantity must reject control characters in quantity names"),
        ("RequireXmlText", "RemoveQuantity must enforce XML-safe canonical quantity keys"),
        ("Quantities.Remove(key)", "RemoveQuantity must remove only the canonical quantity key"),
        ("MarkDirtyCore(ElementDirtyFlags.Quantity, false)", "RemoveQuantity must mark element quantity lifecycle dirty when removal occurs"),
        ("return false", "RemoveQuantity must preserve no-op semantics when the quantity is absent"),
        ("return true", "RemoveQuantity must report a successful lifecycle mutation"),
    ):
        require(remove_quantity, needle, message)

    print("PASS: BLT legacy evidence mutations use ProjectElement property/quantity lifecycle APIs without direct persisted-dictionary bypass.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
