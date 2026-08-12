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
remove_service = read("src/QS3D.BricsCAD.V25/Cad/SemanticTagRemovalService.cs")
command = read("src/QS3D.BricsCAD.V25/SemanticTagCommands.cs")
remove_command = read("src/QS3D.BricsCAD.V25/SemanticTagRemovalCommands.cs")
runtime_health = read("src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs")
runtime_aggregator = read("src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs")
release = read("src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs")
tag_health_command = read("src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs")
health_smoke = read("tests/QS3D.Core.SmokeTests/GeneratedSemanticTagHealthSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")
audit_trail = read("src/QS3D.Core/Audit/AuditTrail.cs")
snapshot_state = read("src/QS3D.Core/Persistence/ProjectStateSnapshot.cs")
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
    "ValidatePrevious",
    "CadHandleService.NormalizeHexHandle",
    "OpenMode.ForRead",
    "Refusing destructive replacement before any semantic tag is erased.",
    "Refusing partial destructive replacement.",
    "if (!(entity is MText))",
    "GeneratedGeometryService.RequireMatchingOwnership(entity, project, element",
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot)",
    "var tag = new MText",
    "Contents = EncodePlainMText(rendered)",
    "GeneratedGeometryService.MarkGenerated(document, transaction, tag, project.ProjectId, element.Id, element.Category)",
    "element.Properties[GeneratedSemanticTagHealthService.TextKey] = rendered;",
    "element.Properties[GeneratedSemanticTagHealthService.PositionScopeKey] = GeneratedSemanticTagHealthService.DrawingLocalWcs;",
    '"documentation.semantic-tag.replace"',
    "transaction.Commit();",
    "cadCommitted = true;",
    "rollback.Restore(project)",
    "EncodePlainMText",
]:
    require(builder, token, "native semantic tag builder")

render = builder.find("SemanticTagRenderer.Render(project, element, template)")
validate_previous = builder.find("var previous = ValidatePrevious(document.Database, project, element, ownership);")
erase = builder.find("ErasePrevious(transaction, project, element, previous)")
metadata = builder.find("element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = generatedHandle;")
audit = builder.find('AuditTrail.ForProject(project).Record(')
commit = builder.find("transaction.Commit();", audit)
committed = builder.find("cadCommitted = true;", commit)
restore = builder.find("rollback.Restore(project)", committed)
if min(render, validate_previous, erase, metadata, audit, commit, committed, restore) < 0 or not render < validate_previous < erase < metadata < audit < commit < committed < restore:
    print("[FAIL] native semantic tag builder: render and complete previous-handle validation must precede erase; semantic ownership/audit revision must precede CAD commit and guarded rollback")
    sys.exit(1)
if "project.Touch();" in builder:
    print("[FAIL] native semantic tag builder must rely on AuditTrail.Record as the single project revision owner")
    sys.exit(1)
for forbidden in [
    "allowMissing: true",
    "if (id.IsNull || !id.IsValid) continue;",
    "if (entity == null || entity.IsErased) continue;",
]:
    if forbidden in builder:
        print("[FAIL] native semantic tag builder must fail closed instead of skipping stale previous handles: " + forbidden)
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
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(slot)",
    "GeneratedGeometryService.RequireMatchingOwnership",
    "if (!(entity is MText))",
    "ProjectStateSnapshot.Capture(project)",
    "var cadCommitted = false;",
    "ParseExpectedHandles",
    "CadHandleService.NormalizeHexHandle",
    "ValidateCompleteLiveTagSet",
    "ids.Count != handles.Count",
    "OpenMode.ForRead",
    "Refusing destructive remove before any semantic tag is erased.",
    "Refusing partial destructive remove.",
    '"documentation.semantic-tag.remove"',
    "transaction.Commit();",
    "cadCommitted = true;",
    "rollback.Restore(project)",
]:
    require(remove_service, token, "semantic tag removal service")

remove_validate = remove_service.find("var ids = ValidateCompleteLiveTagSet(document.Database, project, element, ownership, handles);")
remove_write = remove_service.find("OpenMode.ForWrite", remove_validate)
remove_clear = remove_service.find("ClearGeneratedTagMetadata(element);", remove_write)
remove_audit = remove_service.find('AuditTrail.ForProject(project).Record(', remove_clear)
remove_commit = remove_service.find("transaction.Commit();", remove_audit)
remove_committed = remove_service.find("cadCommitted = true;", remove_commit)
remove_restore = remove_service.find("rollback.Restore(project)", remove_committed)
if min(remove_validate, remove_write, remove_clear, remove_audit, remove_commit, remove_committed, remove_restore) < 0 or not remove_validate < remove_write < remove_clear < remove_audit < remove_commit < remove_committed < remove_restore:
    print("[FAIL] semantic tag removal service: complete live-handle validation must precede writes; metadata/audit revision must precede CAD commit and guarded rollback")
    sys.exit(1)
if "project.Touch();" in remove_service:
    print("[FAIL] semantic tag removal service must rely on AuditTrail.Record as the single project revision owner")
    sys.exit(1)

for forbidden in [
    "allowMissing: true",
    "if (id.IsNull || !id.IsValid) continue;",
    "if (entity == null || entity.IsErased) continue;",
]:
    if forbidden in remove_service:
        print("[FAIL] semantic tag removal service must fail closed instead of skipping missing live handles: " + forbidden)
        sys.exit(1)

record_start = audit_trail.find("public void Record(")
clear_start = audit_trail.find("public void Clear()", record_start + 1) if record_start >= 0 else -1
if record_start < 0 or clear_start <= record_start:
    print("[FAIL] semantic tag lifecycle: could not isolate AuditTrail.Record")
    sys.exit(1)
record = audit_trail[record_start:clear_start]
require(record, "_project?.Touch();", "semantic tag audit-owned revision")
require(record, "_events.Add(item);", "semantic tag audit append")
for token in [
    "target.AuditEvents.Clear();",
    "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);",
]:
    require(snapshot_state, token, "semantic tag rollback revision state")

for token in [
    '[CommandMethod("QS3DTAGREMOVE", CommandFlags.Modal)]',
    "SemanticTagRemovalService.Remove(document, project, element)",
]:
    require(remove_command, token, "semantic tag remove command")

for token in [
    "SEMANTIC_TAG_MTEXT_MISSING",
    "SEMANTIC_TAG_MTEXT_TYPE_MISMATCH",
    "SEMANTIC_TAG_MTEXT_OWNERSHIP_MISMATCH",
    "SEMANTIC_TAG_MTEXT_CONTENT_DRIFT",
    "SEMANTIC_TAG_MTEXT_HEIGHT_DRIFT",
    "SEMANTIC_TAG_MTEXT_POSITION_DRIFT",
    "SEMANTIC_TAG_MTEXT_ROTATION_DRIFT",
    "SEMANTIC_TAG_MTEXT_NORMAL_DRIFT",
    "GeneratedGeometryService.HasMatchingOwnership(tag, project, element)",
    "OpenMode.ForRead",
]:
    require(runtime_health, token, "semantic tag live runtime health")
for forbidden in ["Erase()", "OpenMode.ForWrite"]:
    if forbidden in runtime_health:
        print("[FAIL] semantic tag live runtime health must remain read-only: " + forbidden)
        sys.exit(1)

require(runtime_aggregator, "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)", "runtime health aggregator")
require(release, "GeneratedSolidRuntimeHealthService.Inspect(document, project)", "release readiness live health wiring")

for token in [
    '[CommandMethod("QS3DTAGHEALTH", CommandFlags.Modal)]',
    "GeneratedSemanticTagHealthService().Inspect(project)",
    "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
]:
    require(tag_health_command, token, "semantic tag health command")

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
    "QS3DTAGHEALTH",
    "GeneratedSemanticTagHandles",
    "DrawingLocalWcs",
    "GeneratedSemanticTagRuntimeHealthService",
    "QS3DRELEASECHECK",
    "MLeader",
    "sheet/layout",
    "exact-SHA licensed BricsCAD V25",
]:
    require(doc, token, "semantic tag lifecycle docs")

print("[PASS] semantic tag rendering remains bounded/model-linked and native create/refresh/remove/live-health paths preserve complete live-handle prevalidation, audit-owned single revision, guarded ownership, rollback, read-only runtime diagnostics and release wiring; MLeader/sheet/exact-V25 runtime remain explicit gates")
