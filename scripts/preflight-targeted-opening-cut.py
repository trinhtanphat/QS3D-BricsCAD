#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs": [
        "CutLinkedOpenings(document, project, null)",
        "IReadOnlyCollection<string>? openingIds",
        "NormalizeRequestedOpenings(project, openingIds)",
        "requested == null || requested.Contains(x.Id)",
        "Target element is not Door/WallOpening",
        "Target opening is not linked to a host",
        "PhysicalOpeningCutFingerprint",
        "Hãy Build 3D lại host trước khi khoét tiếp.",
    ],
    "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs": [
        'CommandMethod("QS3DCUTOPENINGS"',
        'CommandMethod("QS3DCUTSELECTEDOPENINGS"',
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        "ResolveOpeningIds(previewProject, handles)",
        "ResolveOpeningIds(project, handles)",
        "expectedTargets.SetEquals(currentOpeningIds)",
        "SemanticReferenceHandles.MatchesSelection(x, handles)",
        "OpeningBooleanService.CutLinkedOpenings(document, project, openingIds)",
        "private static void FinalizeUi(Document document, string message)",
        "TryWriteMessage(document",
    ],
    "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs": [
        'CommandMethod("QS3DDRAWDOOR"',
        'CommandMethod("QS3DDRAWOPENING"',
        "AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)",
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        'Button("Khoét Cửa/Lỗ chọn", "QS3DCUTSELECTEDOPENINGS")',
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Content="Khoét Cửa/Lỗ đang chọn" Tag="QS3DCUTSELECTEDOPENINGS"',
        'Content="Khoét tất cả Cửa/Lỗ đã link" Tag="QS3DCUTOPENINGS"',
    ],
    "docs/DIRECT-DRAW-OPENINGS.md": [
        "`QS3DCUTSELECTEDOPENINGS`",
        "only the selected semantic Door/WallOpening set is eligible for physical cut",
        "fails closed and requires `QS3DBUILD3D`/host rebuild first",
        "Direct Draw still **does not automatically call**",
    ],
}

for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing targeted-opening-cut dependency: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing targeted-opening-cut contract: " + needle)

commands = []
command_root = ROOT / "src/QS3D.BricsCAD.V25"
if command_root.is_dir():
    for path in command_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text))
for name in ("QS3DCUTOPENINGS", "QS3DCUTSELECTEDOPENINGS"):
    count = commands.count(name)
    if count != 1:
        errors.append(name + " must be declared exactly once, found " + str(count))

service = ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs"
if service.is_file():
    text = service.read_text(encoding="utf-8")
    if text.count("public static int CutLinkedOpenings(") != 2:
        errors.append("OpeningBooleanService must expose exactly the legacy all-linked overload and the targeted subset overload")
    requested_index = text.find("var requested = NormalizeRequestedOpenings(project, openingIds);")
    linked_index = text.find("var linked = project.Elements")
    transaction_index = text.find("StartTransaction()")
    if min(requested_index, linked_index, transaction_index) < 0 or not (requested_index < linked_index < transaction_index):
        errors.append("Target ids must be normalized/validated and filter the linked set before native transaction mutation")

commands_file = ROOT / "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs"
if commands_file.is_file():
    text = commands_file.read_text(encoding="utf-8")
    selected = text.split('[CommandMethod("QS3DCUTSELECTEDOPENINGS"', 1)[-1]
    selected_entry = selected.split("private static void Execute", 1)[0]
    if "OpeningBooleanService.CutLinkedOpenings(document, project)" in selected_entry:
        errors.append("Selected-opening command must not fall back to the global all-linked cut API")
    for token in (
        "openingIds = ResolveOpeningIds(previewProject, handles)",
        "currentOpeningIds = ResolveOpeningIds(project, handles)",
        "expectedTargets.SetEquals(currentOpeningIds)",
        'Execute(document, openingIds, "QS3DCUTSELECTEDOPENINGS"',
    ):
        if token not in selected_entry:
            errors.append("Selected-opening command missing target freshness/binding token: " + token)

    helper_start = text.find("private static IReadOnlyList<string> ResolveOpeningIds")
    helper_end = text.find("private static bool IsOpening", helper_start + 1)
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    if not helper:
        errors.append("Selected-opening command must expose a bounded ResolveOpeningIds helper")
    else:
        for token in (
            ".Where(IsOpening)",
            "SemanticReferenceHandles.MatchesSelection(x, handles)",
            ".Distinct(StringComparer.OrdinalIgnoreCase)",
            ".OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
        ):
            if token not in helper:
                errors.append("ResolveOpeningIds missing deduplicated semantic Door/WallOpening target contract: " + token)

opening_direct = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs"
if opening_direct.is_file():
    text = opening_direct.read_text(encoding="utf-8")
    if "new AutoHostLinkCommands().AutoLinkHosts()" in text:
        errors.append("Direct Draw Door/Opening must not re-enter broad AutoHost command selection; use exact LinkSingleOpening lifecycle")
    for forbidden in (
        r"OpeningBooleanService\s*\.\s*CutLinkedOpenings\s*\(",
        r"OpeningBooleanCommands\s*\(\s*\)\s*\.\s*(?:CutSelectedOpenings|CutOpenings)\s*\(",
        r"SendStringToExecute\s*\(\s*\"QS3DCUT(?:SELECTED)?OPENINGS\b",
    ):
        if re.search(forbidden, text):
            errors.append("Direct Draw Door/Opening must keep physical boolean explicit until runtime/journal proof: " + forbidden)

print("QS3D targeted opening cut preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: legacy all-linked cut remains available, selected Door/WallOpening ids are helper-deduplicated and freshness-revalidated before targeted mutation, Direct Draw auto-links only its exact created opening, and physical boolean cutting remains explicit.")
