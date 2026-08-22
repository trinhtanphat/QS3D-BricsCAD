#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
COMMANDS = ADAPTER / "SemanticTagCommands.cs"
REMOVAL = ADAPTER / "SemanticTagRemovalCommands.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def region(text, start_token, end_token, label):
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end <= start:
        errors.append("cannot isolate " + label)
        return ""
    return text[start:end]


commands = read(COMMANDS)
removal = read(REMOVAL)

place = region(
    commands,
    "public void PlaceSemanticTag()",
    "[CommandMethod(\"QS3DTAGREFRESH\"",
    "QS3DTAG",
)
place_tokens = (
    "AcquireSourceHandle(document",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
    "ResolveSourceElement(previewProject, sourceHandle)",
    "var placement = PromptPlacement(document)",
    "if (placement == null) return;",
    "ExistingProjectMutationContext.Require(document, \"Semantic Tag\")",
    "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
    "ResolveSourceElement(project, sourceHandle)",
    "string.Equals(element.Id, expectedElementId, StringComparison.OrdinalIgnoreCase)",
    "SemanticTagBuilder.Build(document, project, element",
)
place_positions = [place.find(token) for token in place_tokens]
if any(position < 0 for position in place_positions):
    errors.append("QS3DTAG missing PICKFIRST/source-preview/placement/canonical-rebind lifecycle token")
elif place_positions != sorted(place_positions):
    errors.append("QS3DTAG must acquire source, validate read-only preview, complete placement, then bind/re-resolve canonical project before build")
if "ProjectContextCoordinator.GetOrCreate" in place:
    errors.append("QS3DTAG must not create a replacement project")

refresh = region(
    commands,
    "public void RefreshSemanticTag()",
    "private static string? AcquireSourceHandle",
    "QS3DTAGREFRESH",
)
refresh_tokens = (
    "AcquireSourceHandle(document",
    "if (sourceHandle == null) return;",
    "ExistingProjectMutationContext.Require(document, \"Semantic Tag refresh\")",
    "ResolveSourceElement(project, sourceHandle)",
    "SemanticTagBuilder.Build(document, project, element",
)
refresh_positions = [refresh.find(token) for token in refresh_tokens]
if any(position < 0 for position in refresh_positions):
    errors.append("QS3DTAGREFRESH missing PICKFIRST/canonical lifecycle token")
elif refresh_positions != sorted(refresh_positions):
    errors.append("QS3DTAGREFRESH must finish source acquisition before canonical project binding/build")
if "ProjectContextCoordinator.GetOrCreate" in refresh:
    errors.append("QS3DTAGREFRESH must not create a replacement project")

acquire_source = region(
    commands,
    "private static string? AcquireSourceHandle",
    "private static string? PromptEntityHandle",
    "Semantic Tag source acquisition helper",
)
for token in ("EntitySnapshotReader.ReadCurrentSelection(document)", "return PromptEntityHandle(document, message);"):
    if token not in acquire_source:
        errors.append("Semantic Tag source acquisition helper missing PICKFIRST/fallback token: " + token)
for forbidden in ("ProjectState", "ProjectContextCoordinator", "ExistingProjectMutationContext", "GetOrCreate"):
    if forbidden in acquire_source:
        errors.append("Semantic Tag source acquisition helper must remain project-agnostic: " + forbidden)

prompt_source = region(
    commands,
    "private static string? PromptEntityHandle",
    "private static ProjectElement ResolveSourceElement",
    "Semantic Tag source prompt helper",
)
for forbidden in ("ProjectState", "ProjectContextCoordinator", "ExistingProjectMutationContext", "GetOrCreate"):
    if forbidden in prompt_source:
        errors.append("Semantic Tag source prompt helper must remain project-agnostic: " + forbidden)

remove = region(
    removal,
    "public void RemoveSemanticTag()",
    "private static string? AcquireTagHandle",
    "QS3DTAGREMOVE",
)
remove_tokens = (
    "AcquireTagHandle(document)",
    "if (selectedHandle == null) return;",
    "ExistingProjectMutationContext.Require(document, \"Semantic Tag remove\")",
    "ResolveTagOwner(project, selectedHandle)",
    "SemanticTagRemovalService.Remove(document, project, element)",
)
remove_positions = [remove.find(token) for token in remove_tokens]
if any(position < 0 for position in remove_positions):
    errors.append("QS3DTAGREMOVE missing PICKFIRST/canonical lifecycle token")
elif remove_positions != sorted(remove_positions):
    errors.append("QS3DTAGREMOVE must finish tag/source acquisition before canonical project binding/remove")
if "ProjectContextCoordinator.GetOrCreate" in remove:
    errors.append("QS3DTAGREMOVE must not create a replacement project")

acquire_remove = region(
    removal,
    "private static string? AcquireTagHandle",
    "private static string? PromptTagHandle",
    "Semantic Tag remove acquisition helper",
)
for token in ("EntitySnapshotReader.ReadCurrentSelection(document)", "return PromptTagHandle(document);"):
    if token not in acquire_remove:
        errors.append("Semantic Tag remove acquisition helper missing PICKFIRST/fallback token: " + token)
for forbidden in ("ProjectState", "ProjectContextCoordinator", "ExistingProjectMutationContext", "GetOrCreate"):
    if forbidden in acquire_remove:
        errors.append("Semantic Tag remove acquisition helper must remain project-agnostic: " + forbidden)

prompt_remove = region(
    removal,
    "private static string? PromptTagHandle",
    "private static ProjectElement ResolveTagOwner",
    "Semantic Tag remove prompt helper",
)
for forbidden in ("ProjectState", "ProjectContextCoordinator", "ExistingProjectMutationContext", "GetOrCreate"):
    if forbidden in prompt_remove:
        errors.append("Semantic Tag remove prompt helper must remain project-agnostic: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Semantic Tag place/refresh/remove acquire PICKFIRST or prompted input before canonical existing-project mutation binding; placement uses a read-only preview and stable ProjectId/element/source re-resolution so cancel/stale-project paths fail closed without replacement-project creation.")
