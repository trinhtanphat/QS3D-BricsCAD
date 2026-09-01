from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "DependencyGraph.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DependencyGraphTransientCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
errors = []


def method_between(start_token, end_token):
    start = source.find(start_token)
    end = source.find(end_token, start + 1)
    if start < 0 or end < 0:
        errors.append("cannot locate method boundary: " + start_token)
        return ""
    return source[start:end]


def require_traversal(method, label):
    stable = 'RequireStableKnownCount(elements, knownCount, knownCountSources, "' + label + '")'
    required = [
        "using (var enumerator = elements.GetEnumerator())",
        "while (true)",
        stable,
        "if (!enumerator.MoveNext())",
        "RequireTraversalCapacity(knownCount,",
        "var element = enumerator.Current",
    ]
    for token in required:
        if token not in method:
            errors.append(label + " missing traversal integrity token: " + token)

    if "while (enumerator.MoveNext())" in method:
        errors.append(label + " regressed to caller-controlled MoveNext loop condition")
    if method.count(stable) < 4:
        errors.append(label + " must rebind Count before MoveNext, after success, on termination, and after traversal")

    move = method.find("if (!enumerator.MoveNext())")
    post = method.find(stable, move + 1)
    capacity = method.find("RequireTraversalCapacity(knownCount,", move + 1)
    current = method.find("var element = enumerator.Current", move + 1)
    if min(move, post, capacity, current) < 0 or not (move < post < capacity < current):
        errors.append(label + " must preserve MoveNext -> Count rebound -> capacity -> Current ordering")


rebuild = method_between("public void Rebuild", "public bool TryGetElement")
ordering = method_between("public IReadOnlyList<ProjectElement> TopologicalDirtyOrder", "private static OrderingSnapshot CaptureOrderingSnapshot")
require_traversal(rebuild, "Dependency graph rebuild")
require_traversal(ordering, "Dependency ordering")

required_smoke = [
    "RebuildRejectsTransientGrowthBeforeCurrent",
    "DirtyOrderingRejectsTransientShrinkBeforeCurrent",
    "RebuildRejectsTransientNegativeCountBeforeCurrent",
    "DirtyOrderingRejectsTransientCrossInterfaceConflictBeforeCurrent",
    "StableCountedAndStreamingInputsRemainAccepted",
    "MoveNextCalls",
    "CurrentReads",
]
for token in required_smoke:
    if token not in smoke:
        errors.append("missing deterministic smoke token: " + token)

for hostile in [
    "RebuildRejectsTransientGrowthBeforeCurrent",
    "DirtyOrderingRejectsTransientShrinkBeforeCurrent",
    "RebuildRejectsTransientNegativeCountBeforeCurrent",
    "DirtyOrderingRejectsTransientCrossInterfaceConflictBeforeCurrent",
]:
    start = smoke.find("private static void " + hostile)
    end = smoke.find("private static void ", start + 1)
    body = smoke[start:end if end >= 0 else len(smoke)] if start >= 0 else ""
    if "Equal(0, source.CurrentReads" not in body:
        errors.append(hostile + " must prove rejection before caller Current")
    if "Equal(1, source.MoveNextCalls" not in body:
        errors.append(hostile + " must prove first MoveNext is the rejecting traversal action")

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS DependencyGraph transient known-Count stability source guard")
