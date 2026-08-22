#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs"
RESOLVER = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/WorkspaceCurtainOwnerSelectionSmoke.cs"


def require(text, token, label, errors):
    if token not in text:
        errors.append(f"missing {label}: {token}")


def main():
    errors = []
    for path in (WORKSPACE, RESOLVER, SMOKE):
        if not path.is_file():
            errors.append("missing Workspace Curtain selection contract file: " + str(path.relative_to(ROOT)))
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    workspace = WORKSPACE.read_text(encoding="utf-8")
    resolver = RESOLVER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    method_start = workspace.find("private bool TryResolveSemanticSelection(")
    method_end = workspace.find("private void RestoreMultiSelectionPresentationState()", method_start)
    if method_start < 0 or method_end < 0:
        errors.append("could not isolate Workspace semantic selection method")
        method = ""
    else:
        method = workspace[method_start:method_end]

    for token, label in (
        ("rawHandles.Any(handle => handle.Length == 0)", "blank CAD-reference refusal"),
        ("var requestedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);", "duplicate-reference index"),
        ("if (requestedHandles.Add(handle)) continue;", "duplicate CAD-reference refusal"),
        ("selected = SemanticHandleOwnershipResolver.Resolve(project, rawHandles);", "canonical generated-owner delegation"),
        ("catch (InvalidOperationException ex)", "ownership ambiguity refusal"),
        ("if (selected.Count != rawHandles.Length)", "unknown/same-owner reference refusal"),
        ("elements = selected;", "verified semantic result publication"),
    ):
        require(method, token, label, errors)

    for stale in (
        "SemanticReferenceHandles.GetSelectionAliases",
        "matchesByHandle",
        "foreach (var element in project.Elements)",
    ):
        if stale in method:
            errors.append("Workspace still duplicates canonical ownership resolution: " + stale)

    for token, label in (
        ("GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)", "dynamic generated-owner slots"),
        ("ambiguously owned by semantic elements", "cross-owner ambiguity guard"),
        ("RequireElementOwnershipUnchanged", "selection ownership freshness"),
    ):
        require(resolver, token, label, errors)

    for token, label in (
        ("[ModuleInitializer]", "auto-registered focused regression"),
        ("PanelFrameAndLegacyReferencesResolveTheExistingFamily", "panel/frame/legacy positive coverage"),
        ('"GeneratedCurtainFrameHandles"', "Curtain frame fixture"),
        ('"GeneratedCurtainPanelHandles"', "Curtain panel fixture"),
        ('"PhysicalOpeningCutSolidHandle"', "legacy physical-cut fixture"),
        ("project.FindFamily(resolved[0].FamilyId)", "existing Family resolution"),
        ("UnknownAndAmbiguousPanelOwnershipFailClosed", "unknown/ambiguous refusal coverage"),
        ("MultipleReferencesCollapseToOneCanonicalOwner", "same-owner cardinality basis"),
    ):
        require(smoke, token, label, errors)

    print("QS3D Workspace Curtain owner-selection preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with", len(errors), "error(s).")
        return 1
    print("PASS: Workspace delegates panel/frame/source/generated-host selection to the canonical generated-owner resolver and retains strict blank, duplicate, unknown, ambiguous, and same-owner multi-reference refusal.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
