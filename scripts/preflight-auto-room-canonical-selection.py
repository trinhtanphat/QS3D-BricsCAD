#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
RESOLVER = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/WorkspaceCurtainOwnerSelectionSmoke.cs"


def require(text, token, label, errors):
    if token not in text:
        errors.append("missing " + label + ": " + token)


def main():
    errors = []
    for path in (POLICY, RESOLVER, WORKSPACE, SMOKE):
        if not path.is_file():
            errors.append("missing canonical selection file: " + str(path.relative_to(ROOT)))
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    policy = POLICY.read_text(encoding="utf-8")
    resolver = RESOLVER.read_text(encoding="utf-8")
    workspace = WORKSPACE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for token, label in (
        ("element.SourceHandles.Count == 0", "explicit SourceHandles precedence"),
        ("AutoRoomLifecycle.IsAutoRoom(element)", "Auto Room selection guard"),
        ("AutoRoomLifecycle.BoundarySourceHandlesKey", "boundary provenance slot"),
        ("MaxBoundarySourceHandleCount = 5000", "boundary ownership count ceiling"),
        ("GetCanonicalBoundarySourceHandles(element, boundaryHandles)", "bounded canonical boundary tokenization"),
        ("MaxBoundarySourceHandleCount + 1", "fail-fast bounded split sentinel"),
        ("AutoRoomLifecycle.NormalizeSourceHandles(tokens)", "canonical boundary provenance validation"),
        ("Add(handle, element, AutoRoomLifecycle.BoundarySourceHandlesKey", "fail-closed semantic ownership channel"),
    ):
        require(resolver, token, label, errors)

    if "boundaryHandles.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)" in resolver:
        errors.append("Auto Room semantic selection must not restore unbounded RemoveEmptyEntries boundary tokenization")

    if "BoundarySourceHandles" in policy:
        errors.append("Auto Room boundary provenance must not be promoted into global generated ownership")

    require(
        workspace,
        "SemanticHandleOwnershipResolver.Resolve(project, rawHandles)",
        "Workspace canonical ownership delegation",
        errors,
    )
    if "SemanticReferenceHandles.GetSelectionAliases" in workspace:
        errors.append("Workspace must not rebuild an adapter-local alias index")

    for token, label in (
        ("AutoRoomBoundaryReferencesRemainCanonicalSelectionAliases", "focused Auto Room regression"),
        ('AutoRoom("ROOM-A", "D1;D2")', "boundary alias fixture"),
        ('AutoRoom("ROOM-B", "D2;D3")', "shared-boundary ambiguity fixture"),
        ('explicitRoom.SourceHandles.Add("E1")', "explicit SourceHandles precedence fixture"),
        ('Resolve(explicitProject, new[] { "D4" }).Count != 0', "boundary suppression assertion"),
    ):
        require(smoke, token, label, errors)

    print("QS3D Auto Room canonical selection preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with", len(errors), "error(s).")
        return 1
    print("PASS: canonical semantic selection preserves bounded, canonical Auto Room boundary provenance without promoting shared boundaries to generated ownership or overriding explicit SourceHandles.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
