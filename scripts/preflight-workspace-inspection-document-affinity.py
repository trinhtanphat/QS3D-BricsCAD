#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing WorkspacePanel source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

required = [
    "private Document? _inspectionOwnerDocument;",
    "_inspectionOwnerDocument = null;",
    "_inspectionOwnerDocument = Application.DocumentManager.MdiActiveDocument;",
    "private bool IsInspectionOwnedBy(Document document)",
    "ReferenceEquals(_inspectionOwnerDocument, document)",
]
for needle in required:
    if needle not in text:
        errors.append("Workspace inspection affinity source missing token: " + needle)


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

set_inspection = method_body("public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)", "private void SyncFamilyFromSelection")
if set_inspection and "_inspectionOwnerDocument = Application.DocumentManager.MdiActiveDocument;" not in set_inspection:
    errors.append("SetInspection must bind the exact active Document that owns the published handle payload")

clear_project = method_body("public void ClearProject(string status)", "private void ConfigureWorkspaceInteractions")
if clear_project and "_inspectionOwnerDocument = null;" not in clear_project:
    errors.append("ClearProject must release inspection owner identity")

select_inspection = method_body("private int SelectInspection()", "private int SelectInspectionSemanticSourcesForBuild()")
if select_inspection:
    if "!IsInspectionOwnedBy(doc)" not in select_inspection:
        errors.append("SelectInspection must fail closed when the stored payload belongs to another Document")
    if "Cad.CadHandleService.Select(doc" not in select_inspection:
        errors.append("SelectInspection CAD selection call unexpectedly missing")

select_sources = method_body("private int SelectInspectionSemanticSourcesForBuild()", "private void ApplyFamilyFilter()")
if select_sources:
    if "!IsInspectionOwnedBy(doc)" not in select_sources:
        errors.append("SelectInspectionSemanticSourcesForBuild must fail closed before resolving stale handles against the current project")
    if "ProjectContextCoordinator.TryGetReadOnly(doc" not in select_sources:
        errors.append("semantic source selection current-project lookup unexpectedly missing")

# The owner token is a native Document reference, not a mutable name/path/handle surrogate.
for forbidden in [
    "_inspectionOwnerDocumentName",
    "_inspectionOwnerPath",
    "string _inspectionOwnerDocument",
]:
    if forbidden in text:
        errors.append("inspection ownership must use reference identity, not a mutable string surrogate: " + forbidden)

print("QS3D Workspace inspection document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Workspace inspection payloads are bound to an exact owner Document, reset releases ownership, and both CAD handle-consuming paths fail closed after an MDI switch.")
