#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


renderer = read("src/QS3D.Core/Documentation/SemanticTagRenderer.cs")
renderer_smoke = read("tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs")
health = read("src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs")
comprehensive = read("src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs")
builder = read("src/QS3D.BricsCAD.V25/Cad/SemanticTagBuilder.cs")
command = read("src/QS3D.BricsCAD.V25/SemanticTagCommands.cs")
removal = read("src/QS3D.BricsCAD.V25/Cad/SemanticTagRemovalService.cs")
removal_command = read("src/QS3D.BricsCAD.V25/SemanticTagRemovalCommands.cs")
health_smoke = read("tests/QS3D.Core.SmokeTests/GeneratedSemanticTagHealthSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")
doc = read("docs/SEMANTIC-TAGS.md")

for token in [
    "MaxTemplateLength = 512",
    "MaxRenderedLength = 2048",
    "MaxTokens = 64",
    '"Id"',
    '"Category"',
    '"Family"',
    '"Floor"',
    '"Zone"',
    'token.StartsWith("P:"',
    'token.StartsWith("Q:"',
    "GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
    'key.StartsWith("Generated"',
    'key.StartsWith("QS3D.Generated"',
    'key.StartsWith("PhysicalOpeningCut"',
    "Unsupported semantic tag token",
]:
    require(renderer, token, "semantic tag renderer")

for token in [
    "StableSemanticReferencesRender",
    "OptionalPropertyAndQuantityRender",
    "GeneratedOwnershipCannotLeakIntoTag",
    "UnsupportedTokenFailsClosed",
    "MissingReferenceFailsClosed",
]:
    require(renderer_smoke, token, "semantic tag renderer smoke")

for token in [
    'public const string HandlesKey = "GeneratedSemanticTagHandles"',
    'public const string DrawingLocalWcs = "DrawingLocalWcs"',
    "SemanticTagRenderer.Render(project, element, template)",
    '"SEMANTIC_TAG_TEXT_STALE"',
    '"SEMANTIC_TAG_RENDER_INVALID"',
    '"SEMANTIC_TAG_PROJECT_MISMATCH"',
    '"SEMANTIC_TAG_POSITION_SCOPE_INVALID"',
    '"SEMANTIC_TAG_POSITION_INVALID"',
]:
    require(health, token, "semantic tag health")

for token in ['"SEMANTIC_TAG"', "new GeneratedSemanticTagHealthService().Inspect(project)"]:
    require(comprehensive, token, "comprehensive semantic tag health")

for token in [
    'internal const string TemplatePropertyKey = "SemanticTagTemplate"',
    'internal const string TextHeightPropertyKey = "SemanticTagTextHeightM"',
    "SemanticTagRenderer.Render(project, element, template)",
    "GeneratedHandleOwnershipIndex.Build(project)",
    "ProjectStateSnapshot.Capture(project)",
    "var cadCommitted = false;",
    "if (!(entity is MText))",
    "GeneratedGeometryService.RequireMatchingOwnership(entity, project, element",
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot)",
    "var tag = new MText",
    "Contents = EncodePlainMText(rendered)",
    "GeneratedGeometryService.MarkGenerated(document, transaction, tag, project.ProjectId, element.Id, element.Category)",
    "element.Properties[GeneratedSemanticTagHealthService.TextKey] = rendered;",
    "element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;",
    '"documentation.semantic-tag.replace"',
    "project.Touch();",
    "transaction.Commit();",
    "cadCommitted = true;",
    "rollback.Restore(project)",
    "EncodePlainMText",
]:
    require(builder, token, "native semantic tag builder")

render = builder.find("SemanticTagRenderer.Render(project, element, template)")
erase = builder.find("ErasePrevious(document, transaction, project, element, ownership)")
metadata = builder.find("element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = generatedHandle;")
audit = builder.find('AuditTrail.ForProject(project).Record(')
touch = builder.find("project.Touch();", audit)
commit = builder.find("transaction.Commit();", touch)
if min(render, erase, metadata, audit, touch, commit) < 0 or not render < erase < metadata < audit < touch < commit:
    print("[FAIL] native semantic tag builder: render/validate must precede erase and semantic ownership/audit/revision must precede CAD commit")
    sys.exit(1)
if "Editor.Regen(" in builder or "PaletteCoordinator" in builder:
    print("[FAIL] native semantic tag builder: UI work must remain command-level post-commit")
    sys.exit(1)

for token in [
    '[CommandMethod("QS3DTAG", CommandFlags.Modal)]',
    '[CommandMethod("QS3DTAGREFRESH", CommandFlags.Modal)]',
    "GeneratedHandleOwnershipIndex.Build(project)",
    "generated.TryFindOwner(handle",
    "QS3D-generated output",
    'generatedOwner.Id + "/" + generatedSlot',
    "SourceHandles.Any",
    "RequireSupportedUcs(document)",
    "result.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem)",
    "Math.Atan2",
    "SemanticTagBuilder.StoredWorldPosition(element)",
    "SemanticTagBuilder.StoredRotation(element)",
    "SemanticTagBuilder.Build(document, project, element",
    "document.Editor.Regen();",
    "UI sync warning:",
]:
    require(command, token, "semantic tag commands")

for token in [
    "GeneratedHandleOwnershipIndex.Build(project)",
    "ProjectStateSnapshot.Capture(project)",
    "var cadCommitted = false;",
    "EnsureOwnedBySemanticTag(ownership, element, handle)",
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot)",
    "if (!(entity is MText))",
    "GeneratedGeometryService.RequireMatchingOwnership(entity, project, element",
    "entity.Erase();",
    'x.StartsWith("GeneratedSemanticTag", StringComparison.OrdinalIgnoreCase)',
    '"documentation.semantic-tag.remove"',
    "project.Touch();",
    "transaction.Commit();",
    "cadCommitted = true;",
    "rollback.Restore(project)",
]:
    require(removal, token, "semantic tag removal service")

remove_owner = removal.find("EnsureOwnedBySemanticTag(ownership, element, handle)")
remove_erase = removal.find("entity.Erase();", remove_owner)
remove_clear = removal.find("ClearGeneratedTagMetadata(element);", remove_erase)
remove_audit = removal.find('AuditTrail.ForProject(project).Record(', remove_clear)
remove_touch = removal.find("project.Touch();", remove_audit)
remove_commit = removal.find("transaction.Commit();", remove_touch)
if min(remove_owner, remove_erase, remove_clear, remove_audit, remove_touch, remove_commit) < 0 or not remove_owner < remove_erase < remove_clear < remove_audit < remove_touch < remove_commit:
    print("[FAIL] semantic tag remove: ownership/type/XData erase must precede metadata clear, audit/revision, and CAD commit")
    sys.exit(1)
if "Editor.Regen(" in removal or "PaletteCoordinator" in removal:
    print("[FAIL] semantic tag remove service: UI work must remain command-level post-commit")
    sys.exit(1)

for token in [
    '[CommandMethod("QS3DTAGREMOVE", CommandFlags.Modal)]',
    "GeneratedHandleOwnershipIndex.Build(project)",
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(generatedSlot)",
    "GeneratedSemanticTagHealthService.HandlesKey",
    "Generated object được chọn thuộc",
    "SourceHandles.Any",
    "SemanticTagRemovalService.Remove(document, project, element)",
    "document.Editor.Regen();",
    "UI sync warning:",
]:
    require(removal_command, token, "semantic tag remove command")

for token in [
    "NoMetadataIsOptional",
    "HealthyTagPasses",
    "SemanticChangeMarksRenderedTextStale",
    "GeneratedRuntimeTemplateFailsClosed",
    "OwnerAndPositionCorruptionAreDetected",
    '"SEMANTIC_TAG_TEXT_STALE"',
    '"SEMANTIC_TAG_RENDER_INVALID"',
]:
    require(health_smoke, token, "semantic tag health smoke")

require(registration, "SemanticTagRendererSmoke.Run();", "renderer smoke registration")
require(registration, "GeneratedSemanticTagHealthSmoke.Run();", "health smoke registration")

for token in [
    "QS3DTAG",
    "QS3DTAGREFRESH",
    "QS3DTAGREMOVE",
    "GeneratedSemanticTagHandles",
    "DrawingLocalWcs",
    "QS3DUNTRACK",
    "preserves CAD geometry by contract",
    "MLeader",
    "sheet/layout",
    "exact-SHA licensed BricsCAD V25",
]:
    require(doc, token, "semantic tag lifecycle docs")

print("[PASS] semantic tag rendering remains bounded/model-linked and QS3DTAG/QS3DTAGREFRESH/QS3DTAGREMOVE provide a rollback-safe owned MText create/refresh/remove lifecycle with UCS-aware placement, explicit non-destructive UNTRACK separation, persisted stale health and explicit MLeader/sheet/runtime gates")
