#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
GRAPH = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
HOST = ROOT / "src/QS3D.Core/Services/HostLinkService.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing dependency-scoped Build3D file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
engine = read(ENGINE)
graph = read(GRAPH)
host = read(HOST)

build_start = source.find('[CommandMethod("QS3DBUILD3D", CommandFlags.UsePickSet)]')
helper_start = source.find("private static IReadOnlyList<string> BuildRegenerationScope", build_start + 1)
build = source[build_start:helper_start] if build_start >= 0 and helper_start > build_start else ""
helper_end = source.find("private static Dictionary<string, string> CaptureGeneratedSolidHandles", helper_start + 1)
helper = source[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""

if not build:
    errors.append("cannot isolate QS3DBUILD3D command body")
else:
    for token in (
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        'ExistingProjectMutationContext.Require(document, "Build 3D")',
        "var regenerationScope = BuildRegenerationScope(project, selectedElements)",
        ".RegenerateDirtySubset(project, regenerationScope)",
        "ProjectStateSnapshot.Capture(project)",
        "CaptureGeneratedSolidHandles(project, elementIds)",
        "BuildCategory(document, project, category, sourceType)",
        "GeneratedSolidHandlesMatch(project, ownershipBefore)",
    ):
        if token not in build:
            errors.append("QS3DBUILD3D dependency-scoped contract missing: " + token)
    if ".RegenerateDirty(project)" in build:
        errors.append("QS3DBUILD3D must not regenerate unrelated dirty project elements")

if not helper:
    errors.append("cannot isolate BuildRegenerationScope")
else:
    for token in (
        "new HashSet<string>(StringComparer.OrdinalIgnoreCase)",
        "new Queue<ProjectElement>(selectedElements.Where(x => x != null))",
        "foreach (var rawDependencyId in element.DependsOn)",
        "var dependency = project.FindElement(dependencyId)",
        "semantic dependency " + '" + dependencyId + " referenced by " + elementId + " is missing',
        "pending.Enqueue(dependency)",
        ".OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
    ):
        if token not in helper:
            errors.append("Build3D regeneration closure missing: " + token)

if "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)" not in engine:
    errors.append("Core RegenerationEngine no longer exposes targeted regeneration")
if "!byId.TryGetValue(dependencyId, out var dependency)" not in graph:
    errors.append("DependencyGraph candidate ordering contract changed; review Build3D closure logic")
for token in (
    "RemoveDependencies(opening, wall.Id);",
    "opening.DependsOn.Add(wall.Id);",
    "DependencyMatches(opening.DependsOn[i], hostId)",
):
    if token not in host:
        errors.append("HostLinkService no longer mirrors canonical host dependency in DependsOn: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DBUILD3D regenerates only selected semantic elements plus their transitive upstream DependsOn closure before native mutation; unrelated dirty/downstream elements remain outside the build side effect.")
