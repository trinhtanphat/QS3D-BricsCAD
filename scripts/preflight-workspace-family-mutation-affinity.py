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

    # Duplicate may create a project only when there is no selected Family. Once a
    # Family is selected, the current document/project generation must own that
    # exact object before any transaction starts.
    if "selected == null" not in add or "ProjectContextCoordinator.GetOrCreate(doc)" not in add:
        fail("New-Family path must preserve no-selection project creation behavior")
    if "ExistingProjectMutationContext.Require(doc, \"Nhân bản Family từ Workspace\")" not in add:
        fail("Duplicate path must require an existing current-document project")
    if "ReferenceEquals(basis, selected)" not in add:
        fail("Duplicate path must require exact selected Family object identity in the current project generation")
    if "ReferenceEquals(family, selected)" not in delete:
        fail("Delete path must require exact selected Family object identity in the current project generation")

    require_order(add, "ReferenceEquals(basis, selected)", "ExecuteAtomic(project", "Duplicate path")
    require_order(delete, "ReferenceEquals(family, selected)", "ProjectFamilyService.ReferenceCount(project", "Delete path")
    require_order(delete, "ReferenceEquals(family, selected)", "ExecuteAtomic(project", "Delete path")

    # The affinity fence itself must be validation-only. Activating a stale Family
    # before ExecuteAtomic would change metadata before the rollback snapshot.
    for block, context in ((add, "Duplicate path"), (delete, "Delete path")):
        fence_at = block.find("ReferenceEquals(")
        atomic_at = block.find("ExecuteAtomic(project")
        prefix = block[:atomic_at] if atomic_at >= 0 else block
        if "_viewModel.SetActiveFamily" in prefix or "ProjectFamilyActivationService.SetActive" in prefix:
            fail(context + " must not activate Family before the atomic rollback snapshot")
        if fence_at < 0:
            continue
        if "RefreshProject();" not in block[fence_at:]:
            fail(context + " stale-generation rejection must reconcile Workspace")
        if "return;" not in block[fence_at:]:
            fail(context + " stale-generation rejection must return before mutation")

    # Stable Id lookup is allowed only to obtain the current project's candidate;
    # null-only validation is not sufficient because a new generation may reuse Id.
    if "selected != null && basis == null" in add:
        fail("Duplicate path still accepts a different current-generation Family with the same stable Id")
    if "project.FindFamily(selected.Id)\n                    ??" in delete:
        fail("Delete path still accepts a different current-generation Family with the same stable Id")

    print("QS3D Workspace Family mutation affinity preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with %d error(s)." % len(errors))
        return 1
    print("PASS: Workspace Family duplicate/delete validate exact project-generation identity before mutation without pre-snapshot activation side effects.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
