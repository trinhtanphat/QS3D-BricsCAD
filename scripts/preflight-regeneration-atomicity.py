#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationAtomicitySmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (ENGINE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing regeneration atomicity contract file: " + str(path.relative_to(ROOT)))

if ENGINE.is_file():
    text = ENGINE.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Persistence;",
        "RegenerateTransactional(project, project.Elements, project.Elements.Count)",
        "RegenerateTransactional(project, targets, targets.Count)",
        "ProjectStateSnapshot.Capture(project)",
        "snapshot.Restore(project);",
        "throw new AggregateException(\"Semantic regeneration failed and project rollback also failed.\"",
        "var expectedElements = project.Elements.ToArray();",
        "RequireElementStructureFresh(project, expectedElements);",
        "return Regenerate(project, candidates, passBasis, expectedElements);",
        "private static void RequireRegenerationStructureFresh",
        "Project element structure changed during regeneration.",
        "RequireRegenerationStructureFresh(project, expectedElements);",
        "var candidateList = candidates?.ToList()",
    ):
        if token not in text:
            errors.append("RegenerationEngine.cs missing atomic/type-safe/structure-safe token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "FailedBatchRestoresWholeProjectSnapshot();",
        "MutateThenFailRegenerator",
        'throw new InvalidOperationException("synthetic regeneration failure")',
        'project.Metadata["Transient"] = element.Id;',
        "restoredFirst.Properties.ContainsKey(\"Probe\")",
        "project.UpdatedUtc != beforeUpdated",
        "AddedElementDuringRegenerationRollsBack();",
        "SameCountReplacementDuringRegenerationRollsBack();",
        "StableRegeneratorStillSucceeds();",
        "AddElementRegenerator",
        "ReplaceElementRegenerator",
        "project.Elements.Add(new ProjectElement(\"Injected\"",
        "project.Elements[0] = replacement;",
    ):
        if token not in text:
            errors.append("RegenerationAtomicitySmoke.cs missing rollback/structure-drift regression token: " + token)

if REG.is_file() and "RegenerationAtomicitySmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("regeneration atomicity smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] semantic regeneration is statically guarded for type-safe candidate enumeration, stable element membership/order/reference structure, and whole-project rollback on mid-batch failure")
