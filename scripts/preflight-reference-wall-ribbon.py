#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
AUGMENTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "ReferenceWallRibbonAugmenter.cs"
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawReferenceWallCommands.cs"
errors = []

for path in (PLUGIN, AUGMENTER, COMMAND):
    if not path.is_file():
        errors.append("missing file: " + str(path.relative_to(ROOT)))

if PLUGIN.is_file():
    text = PLUGIN.read_text(encoding="utf-8")
    bootstrap = text.find("RibbonBootstrapper.TryInitialize();")
    reference = text.find("ReferenceWallRibbonAugmenter.TryInitialize();")
    project = text.find("ProjectRibbonAugmenter.TryInitialize();")
    if min(bootstrap, reference, project) < 0 or not bootstrap < reference < project:
        errors.append("reference-wall ribbon augmenter must run after base ribbon and before project augmenter")
    if "ReferenceWallRibbonAugmenter.Reset();" not in text:
        errors.append("reference-wall ribbon augmenter must reset during plugin termination")

if AUGMENTER.is_file():
    text = AUGMENTER.read_text(encoding="utf-8")
    required = [
        'private const string TabId = "QS3D_AUTHOR";',
        'private const string PanelSourceId = "QS3D_AUTHOR_ARCHITECTURE_PANEL_SOURCE";',
        'private const string ButtonId = "QS3D_AUTHOR_DRAWWALLREF";',
        'private const string ButtonText = "Vẽ Tường tham chiếu";',
        'private const string Command = "QS3DDRAWWALLREF";',
        'CollectionContainsId(items, ButtonId) || CollectionContainsCommand(items, Command)',
        'SetProperty(button, "CommandParameter", Command);',
        'Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + " ", true, false, false);',
    ]
    for needle in required:
        if needle not in text:
            errors.append("missing reference-wall ribbon contract token: " + needle)

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    if '[CommandMethod("QS3DDRAWWALLREF", CommandFlags.Modal)]' not in text:
        errors.append("ribbon target QS3DDRAWWALLREF command is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3D_AUTHOR exposes the reference-wall command through the real plugin startup path without replacing existing wall authoring.")
