#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SemanticTagCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

for command in ("QS3DTAG", "QS3DTAGREFRESH"):
    expected = f'[CommandMethod("{command}", CommandFlags.Modal | CommandFlags.UsePickSet)]'
    if expected not in text:
        raise SystemExit(f"{command} must preserve PICKFIRST via CommandFlags.UsePickSet")

required = (
    "private static string? AcquireSourceHandle(Document document, string message)",
    "var implied = EntitySnapshotReader.ReadCurrentSelection(document);",
    "if (implied.Count > 1)",
    "if (implied.Count == 1)",
    "return PromptEntityHandle(document, message);",
    "var sourceHandle = AcquireSourceHandle(document,",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
    "var placement = PromptPlacement(document);",
    'ExistingProjectMutationContext.Require(document, "Semantic Tag")',
    'ExistingProjectMutationContext.Require(document, "Semantic Tag refresh")',
    "GeneratedHandleOwnershipIndex.Build(project)",
)
missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("Semantic Tag PICKFIRST contract missing: " + " | ".join(missing))

helper = text.index("private static string? AcquireSourceHandle(Document document, string message)")
implied = text.index("var implied = EntitySnapshotReader.ReadCurrentSelection(document);", helper)
multiple = text.index("if (implied.Count > 1)", implied)
single = text.index("if (implied.Count == 1)", multiple)
fallback = text.index("return PromptEntityHandle(document, message);", single)
get_entity = text.index("document.Editor.GetEntity(new PromptEntityOptions(message))", fallback)
if not (helper < implied < multiple < single < fallback < get_entity):
    raise SystemExit("PICKFIRST must remain before the explicit GetEntity fallback")

place_method = text.index("public void PlaceSemanticTag()")
place_acquire = text.index("var sourceHandle = AcquireSourceHandle(document,", place_method)
preview = text.index("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)", place_acquire)
placement = text.index("var placement = PromptPlacement(document);", preview)
place_bind = text.index('ExistingProjectMutationContext.Require(document, "Semantic Tag")', placement)
place_build = text.index("SemanticTagBuilder.Build(document, project, element", place_bind)
if not (place_acquire < preview < placement < place_bind < place_build):
    raise SystemExit("QS3DTAG must complete source + placement input before canonical bind/native build")

refresh_method = text.index("public void RefreshSemanticTag()")
refresh_acquire = text.index("var sourceHandle = AcquireSourceHandle(document,", refresh_method)
refresh_bind = text.index('ExistingProjectMutationContext.Require(document, "Semantic Tag refresh")', refresh_acquire)
refresh_build = text.index("SemanticTagBuilder.Build(document, project, element", refresh_bind)
if not (refresh_acquire < refresh_bind < refresh_build):
    raise SystemExit("QS3DTAGREFRESH must complete source selection before canonical bind/native build")

for forbidden in ("GetOrCreate(document)", "ProjectContextCoordinator.GetOrCreate"):
    if forbidden in text:
        raise SystemExit("Semantic Tag PICKFIRST introduced a forbidden project bootstrap path: " + forbidden)

print("PASS: Semantic Tag commands consume exactly-one PICKFIRST source, preserve explicit fallback, fail closed on multiple selection, and keep user input before canonical mutation/native build.")
