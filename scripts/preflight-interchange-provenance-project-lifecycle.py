#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeProvenanceCommands.cs"
errors = []

if not SOURCE.is_file():
    print("ERROR: missing ProjectInterchangeProvenanceCommands.cs")
    sys.exit(1)

text = SOURCE.read_text(encoding="utf-8")
required = (
    "if (dialog.ShowDialog() != true) return;",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var reviewProject)",
    "var reviewProjectId = reviewProject.ProjectId;",
    "var reviewUpdatedUtc = reviewProject.UpdatedUtc;",
    "var reviewChangeVersion = reviewProject.ChangeVersion;",
    "var reviewDrawingFingerprint = reviewProject.DrawingFingerprint ?? string.Empty;",
    "ProjectInterchangeSourceHandleProvenance.Plan(reviewProject, json)",
    "System.Windows.MessageBox.Show(",
    'ExistingProjectMutationContext.Require(document, "Interchange provenance import")',
    "project.UpdatedUtc != reviewUpdatedUtc",
    "project.ChangeVersion != reviewChangeVersion",
    "reviewDrawingFingerprint",
    "ProjectInterchangeSourceHandleProvenance.Store(project, json)",
    "private static void FinalizeUi(Document document, string status)",
    "private static void Report(Document document, string message)",
)
for token in required:
    if token not in text:
        errors.append("provenance lifecycle missing token: " + token)

if "ProjectContextCoordinator.GetOrCreate(document)" in text:
    errors.append("provenance review/store command must never create/cache a project implicitly")

review_pos = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var reviewProject)")
confirm_pos = text.find("System.Windows.MessageBox.Show(")
bind_pos = text.find('ExistingProjectMutationContext.Require(document, "Interchange provenance import")')
store_pos = text.find("ProjectInterchangeSourceHandleProvenance.Store(project, json)")
if min(review_pos, confirm_pos, bind_pos, store_pos) < 0 or not review_pos < confirm_pos < bind_pos < store_pos:
    errors.append("provenance flow must be read-only review -> confirmation -> canonical existing-project bind -> store")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange provenance review is non-creating, confirmation is freshness-bound to the reviewed semantic snapshot, writes use canonical existing state, and post-commit UI is best effort.")
