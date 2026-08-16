#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTEXT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DocumentContext.cs"
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
errors = []


def require(text, token, message):
    if token not in text:
        errors.append(message)


def forbid(text, token, message):
    if token in text:
        errors.append(message)


def require_order(text, tokens, message):
    positions = [text.find(token) for token in tokens]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append(message)


context = CONTEXT.read_text(encoding="utf-8") if CONTEXT.is_file() else ""
palette = PALETTE.read_text(encoding="utf-8") if PALETTE.is_file() else ""
if not context:
    errors.append("missing WorkspacePanel.DocumentContext.cs")
if not palette:
    errors.append("missing PaletteCoordinator.cs")

for token in (
    "private Document? _workspaceContextDocument = Application.DocumentManager.MdiActiveDocument;",
    "internal void RefreshProjectForActiveDocument()",
    "if (!ReferenceEquals(_workspaceContextDocument, document))",
    "_workspaceContextDocument = document;",
    "internal void ClearProjectForUnavailableDocument(string status)",
    "_workspaceContextDocument = null;",
    "private void ResetWorkspaceAuthoringFilters()",
    "_categoryFilter = null;",
    "_familySubtypeFilter = string.Empty;",
):
    require(context, token, "document-scoped Workspace reset contract missing: " + token)

require_order(
    context,
    [
        "if (!ReferenceEquals(_workspaceContextDocument, document))",
        "_workspaceContextDocument = document;",
        "ResetWorkspaceAuthoringFilters();",
        "RefreshProject();",
    ],
    "changed-document reset must run before project refresh",
)
require_order(
    context,
    [
        "_workspaceContextDocument = null;",
        "ResetWorkspaceAuthoringFilters();",
        "ClearProject(status);",
    ],
    "unavailable-project reset must clear document/filter state before clearing UI",
)

require(palette, "_workspacePanel?.RefreshProjectForActiveDocument();", "PaletteCoordinator refresh must use document-scoped Workspace wrapper")
require(palette, "_workspacePanel?.ClearProjectForUnavailableDocument(status);", "Unavailable-project reset must clear document-scoped Workspace state")
forbid(palette, "_workspacePanel?.RefreshProject();", "PaletteCoordinator must not bypass document-scoped Workspace refresh")
forbid(palette, "_workspacePanel?.ClearProject(status);", "PaletteCoordinator must not bypass document-scoped Workspace clear")

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    "ExistingProjectMutationContext",
    "ProjectFamilyService",
    "AuditTrail",
    "SendStringToExecute",
    ".Touch(",
):
    forbid(context, forbidden, "Workspace document-context reset must remain UI-only: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Workspace category/subtype authoring filters are scoped to the active DWG, reset on document/unavailable-project transitions, and preserved for same-document refresh without mutation.")
