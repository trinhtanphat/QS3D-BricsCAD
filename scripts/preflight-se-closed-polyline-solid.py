#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SeClosedPolylineSolidCommands.cs"
PREFIX = "[preflight-se-closed-polyline-solid]"


def fail(message: str) -> int:
    print(f"{PREFIX} ERROR: {message}")
    return 1


def require_token(source: str, token: str, contract: str) -> None:
    if token not in source:
        raise AssertionError(f"missing {contract}: {token}")


def main() -> int:
    if not SOURCE.is_file():
        return fail(f"missing source file: {SOURCE.relative_to(ROOT)}")

    source = SOURCE.read_text(encoding="utf-8")

    required = [
        ('[CommandMethod("SE", CommandFlags.Modal | CommandFlags.UsePickSet)]', "SE command registration"),
        ('ProjectContextCoordinator.TryGetReadOnly(document, out var observedProject)', "read-only existing-project observation before selection"),
        ('ProjectFamilyActivationService.GetActive(observedProject)', "active Family/Type observation"),
        ('SupportedCategories.Contains(observedFamily.Category)', "supported active-family category gate"),
        ('EntitySnapshotReader.ReadCurrentSelection(document)', "pick-first/current selection snapshot"),
        ('ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)', "active drawing freshness guard"),
        ('ExistingProjectMutationContext.Require(document, "SE")', "existing-project mutation context"),
        ('project.ProjectId, expectedProjectId', "project identity freshness guard"),
        ('project.ChangeVersion != expectedChangeVersion', "project change-version freshness guard"),
        ('RequireSameActiveFamily(activeFamily, expectedFamilyId, expectedCategory)', "active Family/Type freshness guard"),
        ('.GroupBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)', "source handle deduplication"),
        ('sourceIds.Add(RequireClosedPolylineSource(document, snapshot.Handle))', "whole-batch source validation before mutation"),
        ('var batchRollback = ProjectStateSnapshot.Capture(project);', "whole-batch project rollback snapshot"),
        ('batchRollback.Restore(project)', "whole-batch project rollback restore"),
        ('CadHandleService.Resolve(document, new[] { handle })', "live source handle resolution"),
        ('entity is Polyline polyline', "POLYLINE source type gate"),
        ('entity.IsErased', "live source gate"),
        ('polyline.Closed', "closed POLYLINE gate"),
        ('var normal = polyline.Normal;', "2D source plane inspection"),
        ('Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9', "XY-plane normal gate"),
        ('blockTable[BlockTableRecord.ModelSpace]', "Model Space ownership gate"),
        ('SemanticCaptureActiveFamilyAdapter.CaptureSnapshot(document, project, snapshot, activeFamily!)', "active-family semantic capture"),
        ('document.Editor.SetImpliedSelection(sourceIds.ToArray())', "whole-batch native-builder selection"),
        ('var built = StructuralSolidBuilder.BuildSelected(document, project, expectedCategory);', "single native-builder batch call"),
        ('if (built != sources.Count)', "exact batch output-count contract"),
        ('RestoreSelection(document, originalHandles)', "source selection restoration"),
        ('created native solids atomically; source polylines were retained.', "atomic/source-retention user contract"),
    ]

    try:
        for token, contract in required:
            require_token(source, token, contract)

        for category in ("Slab", "Foundation", "Stair", "Earthwork", "Column"):
            require_token(source, f"ElementCategory.{category}", f"supported {category} Family category")
    except AssertionError as exc:
        return fail(str(exc))

    validation_index = source.find('sourceIds.Add(RequireClosedPolylineSource(document, snapshot.Handle))')
    rollback_index = source.find('var batchRollback = ProjectStateSnapshot.Capture(project);')
    capture_index = source.find('SemanticCaptureActiveFamilyAdapter.CaptureSnapshot(document, project, snapshot, activeFamily!)')
    builder_index = source.find('var built = StructuralSolidBuilder.BuildSelected(document, project, expectedCategory);')
    if not (0 <= validation_index < rollback_index < capture_index < builder_index):
        return fail("SE must validate every profile before semantic mutation and call the native builder only after batch capture")

    if source.count('StructuralSolidBuilder.BuildSelected(document, project, expectedCategory)') != 1:
        return fail("SE must invoke the native structural builder exactly once for the full batch")

    if re.search(r"\.\s*Erase\s*\(", source):
        return fail("SE must retain source polylines; direct Entity.Erase() is forbidden")

    if re.search(r"\.\s*Save\w*\s*\(", source) or re.search(r"\bSave(?:Project|CurrentProject|All)?\w*\s*\(", source):
        return fail("SE must not auto-save the QS3D project")

    print(f"{PREFIX} PASS: SE closed-polyline atomic batch contract is guarded.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
