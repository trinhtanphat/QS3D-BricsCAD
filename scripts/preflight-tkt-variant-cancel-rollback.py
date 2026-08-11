from pathlib import Path

text = Path("src/QS3D.BricsCAD.V25/TktVariantCommands.cs").read_text(encoding="utf-8")
start = text.find("private static void Capture(ElementCategory category, string label)")
end = text.find("private static void RestoreVariantOrThrow", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL: TKT Capture lifecycle boundary missing")
body = text[start:end]

tokens = [
    "EntitySnapshotReader.ReadCurrentSelection(document)",
    "if (snapshots.Count == 0)",
    "ProjectContextCoordinator.TryGetReadOnly(document, out _)",
    "ProjectContextCoordinator.GetOrCreate(document)",
    "ProjectStateSnapshot.Capture(project)",
    "ProjectFamilyActivationService.SetActive(project, family.Id)",
    "SemanticCaptureService.CaptureSnapshot(document, snapshot, category)",
]
positions = []
for token in tokens:
    pos = body.find(token)
    if pos < 0:
        raise SystemExit(f"FAIL: missing {token}")
    positions.append(pos)
if positions != sorted(positions):
    raise SystemExit("FAIL: selection/setup/capture ordering regressed")

empty_start = body.find("if (snapshots.Count == 0)")
empty_end = body.find("var projectExistedBeforeCapture", empty_start)
if "GetOrCreate" in body[empty_start:empty_end] or "SetActive" in body[empty_start:empty_end]:
    raise SystemExit("FAIL: empty/cancel selection may mutate project/family state")
if "SemanticCaptureService.Capture(document, category)" in body:
    raise SystemExit("FAIL: TKT wrapper must not prompt/read selection a second time")
if "RestoreVariantOrThrow" not in body:
    raise SystemExit("FAIL: outer batch rollback missing")

restore_start = text.find("private static void RestoreVariantOrThrow")
ui_start = text.find("private static void FinalizeUi", restore_start)
restore = text[restore_start:ui_start]
for token in ("rollback.Restore(project)", "if (!projectExistedBeforeCapture)", "ProjectContextCoordinator.Forget(document)"):
    if token not in restore:
        raise SystemExit(f"FAIL: rollback contract missing {token}")

success_ui = body.rfind("FinalizeUi(document")
catch_pos = body.find("catch (System.Exception operationError)")
if success_ui < catch_pos:
    raise SystemExit("FAIL: post-success UI remains inside business rollback boundary")

print("PASS: TKT variant selection is cancel-safe and Family/capture batch rollback is atomic")
