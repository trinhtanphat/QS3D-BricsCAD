#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/DependencyHealthService.cs": [
        "DEPENDENCY_SELF_REFERENCE",
        "DEPENDENCY_CYCLE",
        "FindCycleMembers",
        "activePath",
        "activeIndex",
        "dependencyState == 0",
        "dependencyState != 1",
        "duplicateIds.Contains(dependencyId)",
        "uniqueIds.Contains(dependencyId)",
        "DEPENDENCY_TARGET_AMBIGUOUS",
        "graph.ContainsKey(element.Id)",
    ],
    "tests/QS3D.Core.SmokeTests/DependencyHealthSmoke.cs": [
        "AcyclicChainPasses",
        "SelfReferenceIsReported",
        "MultiElementCycleReportsOnlyCycleMembers",
        "MissingDependencyIsNotMisclassifiedAsCycle",
        "DuplicateDependencyTargetIsReportedAsAmbiguous",
        'x.Code == "DEPENDENCY_TARGET_AMBIGUOUS"',
        'downstream.DependsOn.Add("A")',
        'ElementId == "D"',
    ],
    "tests/QS3D.Core.SmokeTests/DependencyHealthRegistration.cs": [
        "ModuleInitializer",
        "DependencyHealthSmoke.Run();",
    ],
    "src/QS3D.BricsCAD.V25/HealthAllCommands.cs": [
        "new DependencyHealthService().Inspect(project)",
    ],
    "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs": [
        "new DependencyHealthService().Inspect(project)",
        'CommandMethod("QS3DRELEASECHECK"',
    ],
    "src/QS3D.Core/Services/DependencyGraph.cs": [
        "Dependency cycle detected at",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing dependency-health file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing dependency-health guard/token: " + needle)

service = ROOT / "src/QS3D.Core/Diagnostics/DependencyHealthService.cs"
if service.is_file():
    text = service.read_text(encoding="utf-8")
    if "FindCycleMembers(graph)" not in text:
        errors.append("DependencyHealthService must inspect the complete existing-element graph")
    if "new DependencyGraph" in text or "TopologicalDirtyOrder" in text:
        errors.append("Dependency health must not depend on dirty-only regeneration ordering")
    if "ToDictionary(x => x.Id" in text:
        errors.append("Dependency health must remain inspectable even if in-memory duplicate element ids exist")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic dependency self-references/cycles are detected independently of dirty regeneration, missing refs stay recovery-visible, and Health All/Release Check include the blocker.")
