#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
CORE = ROOT / "src/QS3D.Core/Revisions/SemanticChangeReview.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing revision semantic review UI file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


xaml = read(XAML)
code = read(CODE)
commands = read(COMMANDS)
core = read(CORE)

for token in (
    'x:Name="Tabs"',
    'Header="Khối lượng"',
    'Header="Ngữ nghĩa"',
    'x:Name="SemanticGrid"',
    'Binding="{Binding ElementId}"',
    'Binding="{Binding IdentityChangeCount}"',
    'Binding="{Binding PropertyChangeCount}"',
    'Binding="{Binding QuantityChangeCount}"',
    'Binding="{Binding OmittedSourceReferenceChangeCount}"',
    'Text="SEMANTIC + QUANTITY"',
):
    if token not in xaml:
        errors.append("RevisionWindow XAML missing semantic review token: " + token)

for token in (
    "new SemanticChangeReviewBuilder().Build(before, after)",
    "SemanticGrid.ItemsSource = _semanticReview.Elements",
    "DocumentBoundWindowLifetime.Attach(this, _document)",
    "if (_rows.Count == 0 && _semanticReview.HasChanges) Tabs.SelectedIndex = 1",
    "SemanticGrid.SelectedItem is SemanticChangeReviewElement row",
    "new QuantityRevisionRow { ElementId = row.ElementId",
    "EnsureActive();",
    "OmittedSourceReferenceChangeCount",
):
    if token not in code:
        errors.append("RevisionWindow code-behind missing semantic/freshness token: " + token)

for forbidden in (
    "SourceHandles",
    "CadHandleService",
    "OpenMode.ForWrite",
    "StartTransaction(",
):
    if forbidden in xaml or forbidden in code:
        errors.append("Revision semantic UI must not expose raw handles or native write paths: " + forbidden)

for token in (
    "LocateCurrentElement(doc, row.ElementId, \"Revision Locate\")",
    "var currentProject = ProjectContextCoordinator.GetOrCreate(document);",
    "var element = currentProject.FindElement(elementId)",
    "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
):
    if token not in commands:
        errors.append("ReviewCommands current-state locate contract missing token: " + token)

if "new RevisionService().Compare(before, after)" not in core:
    errors.append("semantic review UI must remain backed by RevisionService.Compare through the Core review builder")

print("QS3D revision semantic review UI preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RevisionWindow presents quantity and grouped semantic changes, hides raw source handles, and reuses the current-state stable-ID locate path without native mutation.")
