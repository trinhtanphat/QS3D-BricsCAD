#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticViewPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewKindValidationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewKindValidationSmokeRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_source = [
    "var viewKind = RequiredKind(definition.Kind);",
    "return new SemanticViewPlan(viewId, viewName, viewKind, floorId, zoneId, selectedIds);",
    "private static SemanticViewKind RequiredKind(SemanticViewKind kind)",
    "if (!Enum.IsDefined(typeof(SemanticViewKind), kind))",
    "throw new InvalidOperationException(\"Unsupported semantic view kind '\" + kind + \"'.\");",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing semantic view kind source contract: {marker}")

if "new SemanticViewPlan(viewId, viewName, definition.Kind, floorId, zoneId, selectedIds)" in source:
    raise SystemExit("legacy unvalidated SemanticViewKind propagation remains")

required_smoke = [
    "UndefinedKindFailsClosed();",
    "DefinedKindsRemainAccepted();",
    "(SemanticViewKind)999",
    "Unsupported semantic view kind '999'.",
    "AssertAccepted(SemanticViewKind.Model);",
    "AssertAccepted(SemanticViewKind.Plan);",
    "AssertAccepted(SemanticViewKind.Schedule);",
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing semantic view kind smoke contract: {marker}")

if "SemanticViewKindValidationSmoke.Run();" not in registration:
    raise SystemExit("Semantic View kind validation smoke is not registered")

print("semantic view kind validation preflight: PASS")
