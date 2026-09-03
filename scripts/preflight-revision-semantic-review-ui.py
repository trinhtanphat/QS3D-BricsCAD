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
    "new SemanticChangeReviewBuilder().Build(before, _afterSnapshot)",
    "SemanticGrid.ItemsSource = _semanticReview.Elements",
    "DocumentBoundWindowLifetime.Attach(this, document)",
    "if (_rows.Count == 0 && _semanticReview.HasChanges) Tabs.SelectedIndex = 1",
    "SemanticGrid.SelectedItem is SemanticChangeReviewElement row",
    "new QuantityRevisionRow { ElementId = row.ElementId",
    "var document = EnsureActiveAndCurrent();",
    "LocateCurrentElement(document, row);",
    "OmittedSourceReferenceChangeCount",
):
    if token not in code:
        errors.append("RevisionWindow code-behind missing semantic/freshness token: " + token)

for forbidden in (
    "SourceHandles",
    "OpenMode.ForWrite",
    "StartTransaction(",
):
    if forbidden in xaml or forbidden in code:
        errors.append("Revision semantic UI must not expose raw handles or native write paths: " + forbidden)

# The modeless window now mirrors the canonical read-only locate workflow locally so it can
# pass a freshly resolved Document rather than retain the command callback's captured wrapper.
for token in (
    "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
    "var element = currentProject.FindElement(row.ElementId)",
    "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
    "CadHandleService.Select(document, handles)",
    'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false)',
):
    if token not in code:
        errors.append("RevisionWindow current-state read-only locate contract missing token: " + token)

if "ProjectContextCoordinator.GetOrCreate" in code:
    errors.append("Revision modeless Locate/freshness must not create/cache replacement project state")
if "private readonly Action<QuantityRevisionRow>? _locate" in code or "_locate = locate" in code:
    errors.append("RevisionWindow must not retain the caller callback that captures the command-time Document wrapper")

# Keep the command helper contract pinned so the mirrored modeless locate path cannot silently drift.
for token in (
    'LocateCurrentElement(doc, row.ElementId, "Revision Locate")',
    "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
    "var element = currentProject.FindElement(elementId)",
    "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
):
    if token not in commands:
        errors.append("ReviewCommands current-state read-only locate contract missing token: " + token)

helper_start = commands.find("private static int LocateCurrentElement")
helper_end = commands.find("private static HashSet<string> CollectGeneratedHandles", helper_start)
helper = commands[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
if "ProjectContextCoordinator.GetOrCreate(document)" in helper:
    errors.append("Revision modeless Locate must not create/cache replacement project state")

# Semantic UI review must be backed by one detached Core generation. Pin the semantic
# behavior rather than the obsolete live-input Compare(before, after) implementation detail.
for token in (
    'RevisionSnapshotDetacher.Capture(before, "semantic review before")',
    'RevisionSnapshotDetacher.Capture(after, "semantic review after")',
    'var beforeIndex = Index(beforeSnapshot, "before")',
    'var afterIndex = Index(afterSnapshot, "after")',
    "new RevisionService().Compare(beforeSnapshot, afterSnapshot)",
):
    if token not in core:
        errors.append("semantic review UI Core backing missing detached-generation token: " + token)

if "new RevisionService().Compare(before, after)" in core:
    errors.append("semantic review UI Core backing regressed to live caller-owned snapshots")

print("QS3D revision semantic review UI preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RevisionWindow presents quantity and grouped semantic changes, hides raw source handles, resolves a live source Document for the current-state read-only stable-ID locate path, and remains backed by one detached Core semantic generation without native write mutation.")
