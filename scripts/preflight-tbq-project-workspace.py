#!/usr/bin/env python3
"""Source-safe regression guard for the project-bound V25 TBQ workspace commands.

This guard intentionally avoids BricsCAD runtime dependencies. It pins the persistence
and freshness transaction shape so the command surface cannot silently regress to
project auto-creation, detached persistence, stale mutation, or non-rollback saves.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/TbqProjectWorkspaceCommands.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TbqProjectWorkspaceSmoke.cs"
DOC = ROOT / "docs/TBQ-PROJECT-WORKSPACE-COMMANDS.md"


def fail(message: str) -> None:
    print("FAIL: " + message)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(label + " missing required token: " + token)


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(label + " contains forbidden regression token: " + token)


def main() -> int:
    commands = COMMANDS.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

    for command in (
        "QS3DTBQSTATUS",
        "QS3DTBQRATEREFERENCE",
        "QS3DTBQBUILDUPANALYSIS",
        "QS3DTBQTRADECFA",
        "QS3DTBQBQLIBRARY",
        "QS3DTBQADJUSTPREVIEW",
        "QS3DTBQADJUSTAPPLY",
    ):
        require(commands, '[CommandMethod("' + command + '"', "TBQ V25 commands")

    require(commands, "ExistingProjectMutationContext.Require(document, operation)", "TBQ existing-project bind")
    require(commands, "ProjectContextCoordinator.RequireBackingStoreUnchanged", "TBQ freshness")
    require(commands, "ProjectStateSnapshot.Capture(context.Project)", "TBQ apply rollback boundary")
    require(commands, "context.Workspace.ApplyAdjustment(adjustment, markup)", "TBQ apply mutation")
    require(commands, "ProjectContextCoordinator.Save(document)", "TBQ coordinator save")
    require(commands, "snapshot.Restore(context.Project)", "TBQ rollback")
    require(commands, "ProjectContextCoordinator.Forget(document)", "TBQ uncertain-save cache discard")

    forbid(commands, "ProjectContextCoordinator.GetOrCreate(", "TBQ V25 commands")
    forbid(commands, "new QsdbProjectStore", "TBQ V25 commands")
    forbid(commands, "File.Write", "TBQ V25 commands")
    forbid(commands, "FileStream", "TBQ V25 commands")

    preview_start = commands.index("public void PreviewCostAdjustment()")
    apply_start = commands.index("public void ApplyCostAdjustment()", preview_start)
    preview_region = commands[preview_start:apply_start]
    require(preview_region, 'context.EnsureFresh("TBQ Adjust Cost Preview")', "TBQ preview freshness")
    require(preview_region, "context.Workspace.PreviewAdjustment(adjustment, markup)", "TBQ preview")
    forbid(preview_region, "ApplyAdjustment(", "TBQ preview")
    forbid(preview_region, "ProjectContextCoordinator.Save(", "TBQ preview")
    forbid(preview_region, "ProjectStateSnapshot.Capture", "TBQ preview")

    apply_end = commands.index("private static void Execute", apply_start)
    apply_region = commands[apply_start:apply_end]
    freshness_at = apply_region.index('context.EnsureFresh("TBQ Adjust Cost Apply")')
    snapshot_at = apply_region.index("ProjectStateSnapshot.Capture(context.Project)")
    mutation_at = apply_region.index("context.Workspace.ApplyAdjustment(adjustment, markup)")
    save_at = apply_region.index("ProjectContextCoordinator.Save(document)")
    restore_at = apply_region.index("snapshot.Restore(context.Project)")
    forget_at = apply_region.index("ProjectContextCoordinator.Forget(document)", restore_at)
    if not (freshness_at < snapshot_at < mutation_at < save_at < restore_at < forget_at):
        fail("TBQ apply ordering must remain freshness -> snapshot -> mutation -> coordinator save -> restore -> cache discard")

    require(smoke, "var beforePreviewVersion = project.ChangeVersion;", "TBQ smoke")
    require(smoke, "workspace.PreviewAdjustment(20m, 0m)", "TBQ smoke")
    require(smoke, 'Equal(beforePreviewVersion, project.ChangeVersion, "TBQ preview must not mutate project")', "TBQ smoke")
    require(smoke, 'current.RateReferences.GetMark("R-CONC")', "TBQ smoke")
    require(smoke, "mark.UsedInBillItems", "TBQ smoke")
    require(smoke, "mark.UsedInUnitRates", "TBQ smoke")
    require(smoke, 'Equal("PROJECT", current.Library.LibraryId, "TBQ library id")', "TBQ smoke")
    require(smoke, "current.Library.Entries[0].ReferenceUnitRate", "TBQ smoke")

    require(doc, "a8b881f05f2378c822bba41e862343add9eb908f", "TBQ qualification doc")
    require(doc, "31876172345", "TBQ qualification doc")
    require(doc, "PENDING_LOCAL", "TBQ qualification doc")
    require(doc, "cancelled during the integration debounce", "TBQ qualification doc")

    print("PASS: TBQ project workspace is source-guarded for existing-project bind, freshness, preview purity and rollback-safe save")
    return 0


if __name__ == "__main__":
    sys.exit(main())
