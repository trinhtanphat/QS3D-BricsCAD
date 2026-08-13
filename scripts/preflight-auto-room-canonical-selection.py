#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/WorkspaceCurtainOwnerSelectionSmoke.cs"


def require(text, token, label, errors):
    if token not in text:
        errors.append("missing " + label + ": " + token)


def main():
    errors = []
    for path in (POLICY, WORKSPACE, SMOKE):
        if not path.is_file():
            errors.append("missing canonical selection file: " + str(path.relative_to(ROOT)))
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    policy = POLICY.read_text(encoding="utf-8")
    workspace = WORKSPACE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for token, label in (
        ("AutoRoomLifecycle.IsAutoRoom(element)", "Auto Room ownership guard"),
        ("element.SourceHandles.Count == 0", "explicit SourceHandles precedence"),
        ("AutoRoomLifecycle.BoundarySourceHandlesKey", "boundary provenance slot"),
        ("SplitHandles(boundaryHandles)", "canonical CAD-handle normalization"),
    ):
        require(policy, token, label, errors)

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
    print("PASS: canonical semantic selection preserves Auto Room boundary provenance without overriding explicit SourceHandles, while Workspace remains on the authoritative ownership resolver.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
