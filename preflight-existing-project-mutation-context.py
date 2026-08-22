#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"


def read(name: str) -> str:
    path = SRC / name
    if not path.is_file():
        raise AssertionError(f"missing {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"{label}: missing {needle!r}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"{label}: forbidden {needle!r}")


def main() -> int:
    try:
        context = read("ExistingProjectMutationContext.cs")
        require(context, "ProjectContextCoordinator.TryGetReadOnly(document, out var observed)", "mutation context")
        require(context, "ProjectContextCoordinator.GetOrCreate(document)", "mutation context")
        require(context, "canonical.ProjectId", "mutation context")
        require(context, "expectedProjectId", "mutation context")
        require(context, "ProjectContextCoordinator.Forget(document)", "mutation context")
        require(context, "No mutation was applied", "mutation context")

        table_files = [
            "BqNativeTableCommands.cs",
            "BbsNativeTableCommands.cs",
            "MaterialUsageNativeTableCommands.cs",
            "RoomFinishNativeTableCommands.cs",
            "DoorOpeningNativeTableCommands.cs",
            "SemanticElementTableCommands.cs",
        ]
        for name in table_files:
            text = read(name)
            require(text, "ExistingProjectMutationContext.Require(document, operation)", name)
            require(text, "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", name + " health")
            forbid(
                text,
                "if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))\n                throw new InvalidOperationException(operation",
                name + " mutation helper",
            )

        auto_host = read("AutoHostLinkCommands.cs")
        require(auto_host, "ExistingProjectMutationContext.TryGet(document, out var project)", "Auto Host")
        forbid(auto_host, "if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))", "Auto Host mutation")

        review = read("ReviewCommands.cs")
        require(review, "ExistingProjectMutationContext.TryGet(doc, out var currentProject)", "Recognition Apply")
        require(review, "ExistingProjectMutationContext.TryGet(doc, out var auditProject)", "Recognition skip audit")
        require(review, "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)", "read-only Locate")
        forbid(review, "var currentProject = ProjectContextCoordinator.GetOrCreate(doc);", "Recognition Apply")
        forbid(review, "AuditTrail.ForProject(ProjectContextCoordinator.GetOrCreate(doc))", "Recognition skip audit")

        tags = read("SemanticTagCommands.cs")
        tag_remove = read("SemanticTagRemovalCommands.cs")
        require(tags, "ExistingProjectMutationContext.Require(document, \"Semantic Tag\")", "Semantic Tag create")
        require(tags, "ExistingProjectMutationContext.Require(document, \"Semantic Tag refresh\")", "Semantic Tag refresh")
        require(tag_remove, "ExistingProjectMutationContext.Require(document, \"Semantic Tag remove\")", "Semantic Tag remove")
        forbid(tags, "ProjectContextCoordinator.GetOrCreate(document)", "Semantic Tag mutation")
        forbid(tag_remove, "ProjectContextCoordinator.GetOrCreate(document)", "Semantic Tag remove")
    except AssertionError as exc:
        print("[FAIL] existing-project-mutation-context:", exc, file=sys.stderr)
        return 1

    print("[PASS] existing-project-mutation-context: mutation paths bind an existing canonical project; read-only health/locate remain non-creating")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
