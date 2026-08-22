from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ElementGeometryPolicy.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ElementGeometryPolicyCurtainFrameMaterialOutputScopeSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/ElementGeometryPolicyCurtainFrameMaterialOutputScopeSmokeRegistration.cs").read_text(encoding="utf-8")

for token in (
    'private static readonly ISet<string> GeneratedOutputProperties',
    'private static readonly ISet<string> CurtainGeneratedOutputProperties',
    '"Material"',
    '"CurtainFrameMaterial"',
    'GeneratedOutputProperties.Contains(key)',
    'category == ElementCategory.GlassWall && CurtainGeneratedOutputProperties.Contains(key)',
):
    assert token in source, f"missing curtain frame material output-scope contract: {token}"

generic_start = source.index("private static readonly ISet<string> GeneratedOutputProperties")
curtain_start = source.index("private static readonly ISet<string> CurtainGeneratedOutputProperties", generic_start)
generic_block = source[generic_start:curtain_start]
assert '"Material"' in generic_block, "generic Material output impact was removed"
assert '"CurtainFrameMaterial"' not in generic_block, "CurtainFrameMaterial leaked back into the global output set"

method_start = source.index("public static bool AffectsGeneratedOutput")
method_end = source.index("public static ElementDirtyFlags SemanticCleanFlags", method_start)
method = source[method_start:method_end]
assert method.index("GeneratedOutputProperties.Contains(key)") < method.index(
    "category == ElementCategory.GlassWall && CurtainGeneratedOutputProperties.Contains(key)"
), "generated-output category scope ordering drifted"

for token in (
    "GenericMaterialRemainsOutputAffecting",
    "CurtainFrameMaterialIsGlassWallOnly",
    "CurtainGeometryScopeRemainsGlassWallOnly",
    'ElementCategory.GlassWall, "CurtainFrameMaterial"',
    'ElementCategory.Beam, "CurtainFrameMaterial"',
    'ElementCategory.Slab, "CurtainFrameMaterial"',
    'ElementCategory.Column, "CurtainFrameMaterial"',
    'ElementCategory.ArchitecturalWall, "CurtainFrameMaterial"',
):
    assert token in smoke, f"missing curtain frame material output-scope smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "curtain frame material output-scope smoke is not registered"
assert "ElementGeometryPolicyCurtainFrameMaterialOutputScopeSmoke.Run();" in registration, (
    "curtain frame material output-scope registration drifted"
)

print("PASS: CurtainFrameMaterial generated-output impact is scoped to GlassWall")
