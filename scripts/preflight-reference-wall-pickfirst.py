#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawReferenceWallCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    '[CommandMethod("QS3DDRAWWALLREF", CommandFlags.Modal | CommandFlags.UsePickSet)]',
    '[CommandMethod("QS3DDRAWWALLREFADV", CommandFlags.Modal | CommandFlags.UsePickSet)]',
    'var implied = document.Editor.SelectImplied();',
    'if (objectIds.Length == 1)',
    'ReadReferenceLine(document, objectIds[0], failIfNotLine: false)',
    'var result = document.Editor.GetEntity(options);',
    'ReadReferenceLine(document, result.ObjectId, failIfNotLine: true)',
    'transaction.GetObject(objectId, OpenMode.ForRead) as Line',
    'var projectPreview = DirectDrawProjectPreviewContext.Capture(document);',
    '.RegenerateDirtySubset(project, new[] { createdElementId });',
]

missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("reference-wall PICKFIRST contract missing: " + " | ".join(missing))

acquire = text.index("private static ReferenceLinePlan? AcquireReferenceLine")
preview = text.index("var projectPreview = DirectDrawProjectPreviewContext.Capture(document);")
if acquire < preview:
    pass
else:
    raise SystemExit("reference acquisition must remain before project preview/mutation")

select_implied = text.index("var implied = document.Editor.SelectImplied();", acquire)
get_entity = text.index("var result = document.Editor.GetEntity(options);", acquire)
if select_implied >= get_entity:
    raise SystemExit("PICKFIRST must be attempted before interactive GetEntity fallback")

for forbidden in (
    "RegenerateDirty(project)",
    "GetOrCreate(document)",
    "GetOrCreateProject",
):
    if forbidden in text:
        raise SystemExit("forbidden broad/creating path introduced: " + forbidden)

print("PASS: Direct Draw Reference Wall consumes exactly one preselected LINE before prompt fallback while preserving source-safe scoped authoring.")
