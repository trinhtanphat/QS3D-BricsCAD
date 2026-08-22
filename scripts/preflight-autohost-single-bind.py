#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DAUTOLINKHOSTS", CommandFlags.UsePickSet)]')
    finalize = text.find("private static void FinalizeAutoHostUi", start)
    if start < 0 or finalize <= start:
        errors.append("cannot isolate QS3DAUTOLINKHOSTS")
    else:
        command = text[start:finalize]
        tokens = {
            "selection": "var selected = ReadSelectedHandles(document);",
            "empty": "if (selected.Count == 0)",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
            "preview_targets": "var previewOpenings = ResolveSelectedOpenings(previewProject, selected);",
            "zero": "if (previewOpenings.Count == 0)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "target_ids": "var expectedOpeningIds = new HashSet<string>(",
            "bind": "ExistingProjectMutationContext.TryGet(document, out var project)",
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "canonical_targets": "var openings = ResolveSelectedOpenings(project, selected);",
            "same_targets": "expectedOpeningIds.SetEquals(openings.Select(x => x.Id))",
            "matcher": "var matcher = new OpeningHostMatcher();",
            "snapshot": "ProjectStateSnapshot.Capture(project)",
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("Auto Host single-bind missing token: " + token)

        ordered = (
            "selection", "empty", "readonly", "preview_targets", "zero", "project_id", "version",
            "target_ids", "bind", "fresh_id", "fresh_version", "canonical_targets", "same_targets",
            "matcher", "snapshot",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("Auto Host must resolve selected openings read-only, no-op zero targets, bind once, revalidate project/targets, then match/mutate")

        if command.count("ExistingProjectMutationContext.TryGet(document, out var project)") != 1:
            errors.append("Auto Host batch command must bind canonical mutation context exactly once")
        if "ProjectContextCoordinator.GetOrCreate(document)" in command:
            errors.append("Auto Host batch command must not bootstrap project state")

    helper_start = text.find("private static List<ProjectElement> ResolveSelectedOpenings")
    location_start = text.find("private static OpeningLocation ReadOpeningLocation", helper_start)
    if helper_start < 0 or location_start <= helper_start:
        errors.append("missing ResolveSelectedOpenings helper")
    else:
        helper = text[helper_start:location_start]
        for token in (
            "project.Elements",
            "x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening",
            "x.SourceHandles.Any(selected.Contains)",
            ".OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)",
            ".ToList();",
        ):
            if token not in helper:
                errors.append("ResolveSelectedOpenings missing token: " + token)
        for forbidden in (
            "ExistingProjectMutationContext",
            "ProjectContextCoordinator.GetOrCreate",
            "HostLinkService",
            "project.Touch();",
            "SetProperty(",
        ):
            if forbidden in helper:
                errors.append("ResolveSelectedOpenings must remain read-only: " + forbidden)

    single = text.find("internal static string LinkSingleOpening")
    selected_helper = text.find("private static HashSet<string> ReadSelectedHandles", single)
    if single < 0 or selected_helper <= single:
        errors.append("cannot isolate LinkSingleOpening")
    else:
        body = text[single:selected_helper]
        for token in (
            "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
            "ReferenceEquals(currentProject, project)",
            "new HostLinkService().LinkOpening(project, opening.Id, match.HostElementId);",
            "if (UpdateAutoHostMetadata(opening, match.GapM)) project.Touch();",
            "return match.HostElementId;",
        ):
            if token not in body:
                errors.append("LinkSingleOpening contract drifted: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Auto Host resolves selected openings read-only, no-ops zero targets before canonicalization, pins project/version/target IDs, binds once, revalidates, then preserves matching/rollback/regeneration and exact single-opening lifecycle.")
