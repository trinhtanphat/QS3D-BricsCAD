#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
RESOLVER = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSelectionBoundSmoke.cs"

errors = []

if not SERVICE.is_file():
    errors.append("missing SourceReconcileService.cs")
else:
    text = SERVICE.read_text(encoding="utf-8")
    start = text.find("private static List<Target> ResolveTargets")
    end = text.find("private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets", start)
    body = text[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append("cannot isolate Source Reconcile target resolution")
    else:
        generated_build = body.find("var generatedOwners = GeneratedHandleOwnershipIndex.Build(project);")
        generated_loop = body.find("foreach (var snapshot in snapshots)", generated_build)
        generated_refusal = body.find("generatedOwners.TryFindOwner(snapshot.Handle", generated_loop)
        batch = body.find("var resolvedElements = SemanticHandleOwnershipResolver.Resolve(", generated_refusal)
        selected_handles = body.find("snapshots.Select(x => x.Handle)", batch)
        index = body.find("var sourceOwners = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);", selected_handles)
        owner_loop = body.find("foreach (var element in resolvedElements)", index)
        one_source = body.find("element.SourceHandles.Count != 1", owner_loop)
        owner_identity = body.find("QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(element.SourceHandles[0])", one_source)
        index_add = body.find("sourceOwners.Add(sourceHandle, element);", owner_identity)
        snapshot_loop = body.find("foreach (var snapshot in snapshots)", index_add)
        snapshot_identity = body.find("QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(snapshot.Handle)", snapshot_loop)
        indexed_lookup = body.find("sourceOwners.TryGetValue(sourceHandle, out var element)", snapshot_identity)
        duplicate = body.find("seenElements.Add(element.Id)", indexed_lookup)
        target = body.find("targets.Add(new Target { Snapshot = snapshot, Element = element });", duplicate)
        positions = (
            generated_build,
            generated_loop,
            generated_refusal,
            batch,
            selected_handles,
            index,
            owner_loop,
            one_source,
            owner_identity,
            index_add,
            snapshot_loop,
            snapshot_identity,
            indexed_lookup,
            duplicate,
            target,
        )
        if any(position < 0 for position in positions) or list(positions) != sorted(positions):
            errors.append(
                "Source Reconcile must reject generated output, batch-resolve bounded canonical ownership once, index one-source owners, then map snapshots without rescanning the project"
            )
        if body.count("SemanticHandleOwnershipResolver.Resolve(") != 1:
            errors.append("Source Reconcile must call the canonical batch resolver exactly once")
        for forbidden in (
            "ResolveUniqueSourceOwner",
            "foreach (var element in project.Elements)",
            ".Where(x => x.SourceHandles.Any",
            "BuildSourceOwnerIndex",
        ):
            if forbidden in body:
                errors.append("Source Reconcile target resolution reintroduced per-snapshot/raw ownership work: " + forbidden)

if not RESOLVER.is_file():
    errors.append("missing SemanticHandleOwnershipResolver.cs")
else:
    text = RESOLVER.read_text(encoding="utf-8")
    start = text.find("public static IReadOnlyList<ProjectElement> Resolve(")
    end = text.find("private static HashSet<string> MaterializeSelectedHandles", start)
    body = text[start:end] if start >= 0 and end > start else ""
    for token in (
        "var elementOwnership = SnapshotElementOwnership(project);",
        "var selected = MaterializeSelectedHandles(selectedHandles);",
        "RequireElementOwnershipUnchanged(project, elementOwnership);",
        "foreach (var element in project.Elements)",
    ):
        if token not in body:
            errors.append("canonical batch ownership resolver contract drifted: " + token)
    if "private const int MaxSelectedHandleInputCount = 10000;" not in text:
        errors.append("canonical batch ownership resolver lost its 10,000-entry input ceiling")
    if text.count("if (inputCount >= MaxSelectedHandleInputCount)") != 1:
        errors.append("canonical batch ownership resolver must stop before consuming entry 10,001")

if not SMOKE.is_file():
    errors.append("missing SemanticHandleOwnershipSelectionBoundSmoke.cs")
else:
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "TenThousandRawEntriesRemainSupportedAndNormalized",
        "KnownOversizeSelectionFailsReadOnly",
        "LazyOversizeSelectionStopsAtMaxPlusOneReadOnly",
        "Equal(10001, observed)",
    ):
        if token not in text:
            errors.append("canonical selection-bound smoke missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Source Reconcile rejects generated output first, uses one bounded canonical ownership scan, and maps selected snapshots through an indexed one-source owner set without per-snapshot project rescans.")
