#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []


def fail(message: str) -> None:
    errors.append(message)


def method_body(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        fail("missing method: " + signature)
        return ""
    brace = text.find("{", start)
    if brace < 0:
        fail("missing method body: " + signature)
        return ""
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    fail("unterminated method body: " + signature)
    return text[start:]


def require_order(block: str, first: str, second: str, context: str) -> None:
    first_at = block.find(first)
    second_at = block.find(second)
    if first_at < 0:
        fail(context + " missing: " + first)
    if second_at < 0:
        fail(context + " missing: " + second)
    if first_at >= 0 and second_at >= 0 and first_at > second_at:
        fail(context + " order invalid: " + first + " must precede " + second)


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")
    add = method_body(text, "private void OnAddClick(object sender, RoutedEventArgs e)")
    delete = method_body(text, "private void OnDeleteClick(object sender, RoutedEventArgs e)")
    validate = method_body(text, "private bool TryGetCurrentFamilyForWorkspaceMutation")

    for required, message in [
        ("ExistingProjectMutationContext.TryGet", "mutation affinity helper must resolve the current project from the active document without creating one"),
        ("project.FindFamily(family.Id)", "mutation affinity helper must resolve Family identity in the current project generation"),
        ("ReferenceEquals(ownedFamily, family)", "mutation affinity helper must require exact Family object identity, not stable Id alone"),
    ]:
        if required not in validate:
            fail(message)

    for forbidden, message in [
        ("_viewModel.SetActiveFamily", "mutation affinity validation must not change active Family before transaction snapshot"),
        ("ProjectFamilyActivationService.SetActive", "mutation affinity validation must not mutate ActiveFamily metadata before transaction snapshot"),
        ("GetOrCreate", "mutation affinity validation must never create a project"),
    ]:
        if forbidden in validate:
            fail(message)

    if "TryGetCurrentFamilyForWorkspaceMutation(doc, selected" not in add:
        fail("Duplicate path must validate selected Family against current document/project generation before mutation")
    if "TryGetCurrentFamilyForWorkspaceMutation(doc, selected" not in delete:
        fail("Delete path must validate selected Family against current document/project generation before mutation")

    require_order(add, "TryGetCurrentFamilyForWorkspaceMutation(doc, selected", "ExecuteAtomic(project", "Duplicate path")
    require_order(delete, "TryGetCurrentFamilyForWorkspaceMutation(doc, selected", "ProjectFamilyService.ReferenceCount(project", "Delete path")
    require_order(delete, "TryGetCurrentFamilyForWorkspaceMutation(doc, selected", "ExecuteAtomic(project", "Delete path")

    if "project.FindFamily(selected.Id)" in add:
        fail("Duplicate path still rebinds stale selection by Id after validation; use exact validated selected Family")
    if "project.FindFamily(selected.Id)" in delete:
        fail("Delete path still rebinds stale selection by Id; exact identity validation must own generation affinity")

    for block, context in ((add, "Duplicate path"), (delete, "Delete path")):
        affinity = block.find("TryGetCurrentFamilyForWorkspaceMutation(doc, selected")
        refresh = block.find("RefreshProject();", affinity if affinity >= 0 else 0)
        if affinity < 0 or refresh < 0:
            fail(context + " must reconcile Workspace when selected Family affinity is rejected")
        elif "return;" not in block[affinity:refresh + len("RefreshProject();") + 32]:
            fail(context + " stale-generation branch must return before mutation")

    if "selected == null" not in add or "ProjectContextCoordinator.GetOrCreate(doc)" not in add:
        fail("New-Family path must preserve existing no-selection project creation behavior")

    print("QS3D Workspace Family mutation affinity preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with %d error(s)." % len(errors))
        return 1
    print("PASS: Workspace Family duplicate/delete reject stale project-generation selections before transactional mutation without pre-snapshot activation side effects.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
