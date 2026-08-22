#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"


def read(name):
    path = SRC / name
    if not path.is_file():
        raise AssertionError("missing " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + ": missing " + repr(token))


def forbid(text, token, label):
    if token in text:
        raise AssertionError(label + ": forbidden " + repr(token))


def main():
    try:
        context = read("ExistingProjectMutationContext.cs")
        for token in (
            "ProjectContextCoordinator.TryGetReadOnly(document, out var observed)",
            "ProjectContextCoordinator.GetOrCreate(document)",
            "canonical.ProjectId",
            "expectedProjectId",
            "ProjectContextCoordinator.Forget(document)",
            'ProjectContextCoordinator.RequireBackingStoreUnchanged(document, canonical, "QS3D existing-project mutation")',
        ):
            require(context, token, "mutation context")

        for name in (
            "BqNativeTableCommands.cs", "BbsNativeTableCommands.cs", "MaterialUsageNativeTableCommands.cs",
            "RoomFinishNativeTableCommands.cs", "DoorOpeningNativeTableCommands.cs", "SemanticElementTableCommands.cs",
        ):
            text = read(name)
            require(text, "ExistingProjectMutationContext.Require(document, operation)", name)
            require(text, "ProjectContextCoordinator.TryGetReadOnly(document, out var project)", name + " health")

        auto_host = read("AutoHostLinkCommands.cs")
        require(auto_host, "ExistingProjectMutationContext.TryGet(document, out var project)", "Auto Host")

        review = read("ReviewCommands.cs")
        require(review, "RecognitionApplyBatchService.PrepareStrict(", "Recognition Apply batch preflight")
        require(review, "RecognitionApplyBatchService.Commit(doc, reviewProjectId, plan)", "Recognition Apply atomic commit")
        require(review, "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)", "Recognition/BBS/Revision Locate")

        recognition_apply = read("Services/RecognitionApplyBatchService.cs")
        require(recognition_apply, "ExistingProjectMutationContext.TryGet(document, out var project)", "Recognition Apply existing-project mutation")
        require(recognition_apply, "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)", "Recognition Apply project identity")
        require(recognition_apply, "if (project.ChangeVersion != plan.ProjectChangeVersion)", "Recognition Apply freshness")
        forbid(recognition_apply, "ProjectContextCoordinator.GetOrCreate(document)", "Recognition Apply replacement project")

        tags = read("SemanticTagCommands.cs")
        tag_remove = read("SemanticTagRemovalCommands.cs")
        require(tags, "ExistingProjectMutationContext.Require(document, \"Semantic Tag\")", "Semantic Tag create")
        require(tags, "ExistingProjectMutationContext.Require(document, \"Semantic Tag refresh\")", "Semantic Tag refresh")
        require(tag_remove, "ExistingProjectMutationContext.Require(document, \"Semantic Tag remove\")", "Semantic Tag remove")

        for name, builder in (
            ("UI/DoorOpeningScheduleWindow.xaml.cs", "DoorOpeningScheduleBuilder.Build(snapshot)"),
            ("UI/RoomFinishScheduleWindow.xaml.cs", "RoomFinishScheduleBuilder.Build(snapshot)"),
            ("UI/RebarScheduleWindow.xaml.cs", "ProjectRebarScheduleBuilder.Build(snapshot)"),
        ):
            text = read(name)
            require(text, "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", name)
            require(text, "ProjectStateSnapshot.CreateDetachedCopy(project)", name)
            require(text, "RegenerateDirty(snapshot)", name)
            require(text, builder, name)
            forbid(text, "ExistingProjectMutationContext", name + " read-only review")
            forbid(text, "RegenerateDirty(project)", name + " live mutation")

        bq = read("UI/QuantitySummaryWindow.xaml.cs")
        require(bq, "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", "BQ preference load")
        start = bq.find("private void PersistColumnPreferences()")
        end = bq.find("private IEnumerable<CheckBox>", start)
        persist = bq[start:end]
        require(persist, "ExistingProjectMutationContext.TryGet(_document, out var project)", "BQ preference mutation")
        require(persist, "ProjectStateSnapshot.Capture(project)", "BQ preference rollback")
        forbid(persist, "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", "BQ detached preference mutation")
    except AssertionError as exc:
        print("[FAIL] existing-project-mutation-context:", exc, file=sys.stderr)
        return 1

    print("[PASS] lifecycle: true writes bind canonical existing state; Recognition batch commit is existing-project/version guarded; read-only modeless regeneration uses detached snapshots")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
