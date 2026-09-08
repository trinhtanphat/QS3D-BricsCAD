#!/usr/bin/env python3
"""Keep the executable Core quantity policy connected to both native write paths."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
adapter = root / "src/QS3D.BricsCAD.V25"
contract = (adapter / "SingleFootingContract.cs").read_text(encoding="utf-8")
element_apply = contract.split("public static void Apply(ProjectElement element,", 1)[1].split("private static void WriteDimensions", 1)[0]
assert "SingleFootingQuantityPolicy.Apply(element, dimensions);" in element_apply, "Native clean-state quantities are not refreshed"
for name in ("SingleFootingCommands.cs", "SingleFootingRegenerationService.cs"):
    source = (adapter / name).read_text(encoding="utf-8")
    apply_at = source.index("SingleFootingContract.Apply(element, dimensions);")
    clean_at = source.index("element.MarkClean(", apply_at)
    assert apply_at < clean_at, f"{name}: quantity projection must precede clean state"
structural = (root / "src/QS3D.Core/Services/StructuralRegenerator.cs").read_text(encoding="utf-8")
foundation = structural.split("private static void RegenerateFoundation(", 1)[1].split("private static void RegenerateStair", 1)[0]
assert foundation.index("SingleFootingQuantityPolicy.TryApply(element)") < foundation.index('"BaseAreaM2"'), "Tagged footings fell through to stale prism quantities"
assert "LevelReferenceNativeIntegrationPolicy.EnsureQualified(element" in structural, "Existing Level qualification gate removed"
v26 = (root / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj").read_text(encoding="utf-8")
assert 'Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"' in v26, "V26 no longer shares native footing quantity hook"
print("PASS single footing native creation/edit/dirty quantity policy wiring (source-only)")
