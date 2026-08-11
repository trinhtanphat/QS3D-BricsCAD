#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SemanticTagRemovalCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

expected = '[CommandMethod("QS3DTAGREMOVE", CommandFlags.Modal | CommandFlags.UsePickSet)]'
if expected not in text:
    raise SystemExit("QS3DTAGREMOVE must preserve PICKFIRST via CommandFlags.UsePickSet")

required = (
    "private static string? AcquireTagHandle(Document document)",
    "var implied = EntitySnapshotReader.ReadCurrentSelection(document);",
    "if (implied.Count > 1)",
    "if (implied.Count == 1)",
    "return PromptTagHandle(document);",
    "var selectedHandle = AcquireTagHandle(document);",
    'ExistingProjectMutationContext.Require(document, "Semantic Tag remove")',
    "var element = ResolveTagOwner(project, selectedHandle);",
    "SemanticTagRemovalService.Remove(document, project, element)",
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(generatedSlot)",
    "GeneratedSemanticTagHealthService.HandlesKey",
)
missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("Semantic Tag Remove PICKFIRST contract missing: " + " | ".join(missing))

helper = text.index("private static string? AcquireTagHandle(Document document)")
implied = text.index("var implied = EntitySnapshotReader.ReadCurrentSelection(document);", helper)
multiple = text.index("if (implied.Count > 1)", implied)
single = text.index("if (implied.Count == 1)", multiple)
fallback = text.index("return PromptTagHandle(document);", single)
get_entity = text.index("document.Editor.GetEntity(new PromptEntityOptions", fallback)
if not (helper < implied < multiple < single < fallback < get_entity):
    raise SystemExit("PICKFIRST must remain before explicit remove picker fallback")

method = text.index("public void RemoveSemanticTag()")
acquire = text.index("var selectedHandle = AcquireTagHandle(document);", method)
bind = text.index('ExistingProjectMutationContext.Require(document, "Semantic Tag remove")', acquire)
resolve = text.index("var element = ResolveTagOwner(project, selectedHandle);", bind)
remove = text.index("SemanticTagRemovalService.Remove(document, project, element)", resolve)
if not (acquire < bind < resolve < remove):
    raise SystemExit("Tag remove must complete selection before bind, resolve owner before destructive removal")

for forbidden in ("GetOrCreate(document)", "ProjectContextCoordinator.GetOrCreate"):
    if forbidden in text:
        raise SystemExit("Tag Remove PICKFIRST introduced forbidden project bootstrap: " + forbidden)

print("PASS: QS3DTAGREMOVE consumes exactly-one PICKFIRST selection, preserves explicit fallback, owner validation, selection-before-bind and destructive removal boundaries.")
