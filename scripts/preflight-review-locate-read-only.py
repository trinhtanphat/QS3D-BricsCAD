#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ReviewCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    marker = "private static int LocateCurrentElement(Document document, string elementId, string operation)"
    start = text.find(marker)
    if start < 0:
        errors.append("ReviewCommands.cs missing LocateCurrentElement helper")
    else:
        end = text.find("private static HashSet<string> CollectGeneratedHandles", start)
        block = text[start:end if end >= 0 else len(text)]
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)" not in block:
            errors.append("modeless BBS/Revision Locate must re-resolve the existing project read-only")
        if "ProjectContextCoordinator.GetOrCreate(document)" in block:
            errors.append("modeless Locate must not create/cache project state")
        if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in block:
            errors.append("modeless Locate must remain bound to its source DWG")
        if "currentProject.FindElement(elementId)" not in block:
            errors.append("modeless Locate must re-resolve the semantic element by stable ElementId")
        if "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })" not in block:
            errors.append("modeless Locate must resolve current CAD source handles after re-resolving the element")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] modeless BBS/Revision Locate is DWG-bound, read-only, and re-resolves current semantic state")
