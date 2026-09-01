#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/XrefService.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.XrefLock.cs"
CORE_UI = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
errors = []

for path in (SERVICE, XAML, PARTIAL, CORE_UI):
    if not path.is_file():
        errors.append("missing Xref lock source: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    required = (
        "public static int SetInstanceLayersLocked(Document document, string xrefName, bool locked)",
        "using (document.LockDocument())",
        "using (var transaction = document.Database.TransactionManager.StartTransaction())",
        "var xrefId = FindRecord(document.Database, transaction, xrefName);",
        "var currentSpace = transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForRead, false) as BlockTableRecord;",
        "var layerIds = new HashSet<ObjectId>();",
        "reference == null || reference.IsErased || reference.BlockTableRecord != xrefId",
        "if (!reference.LayerId.IsNull) layerIds.Add(reference.LayerId);",
        "transaction.GetObject(layerId, OpenMode.ForWrite, false) as LayerTableRecord",
        "layer.IsLocked = locked;",
        "transaction.Commit();",
        "if (affectedLayers > 0) document.Editor.Regen();",
        "return affectedLayers;",
    )
    for needle in required:
        if needle not in text:
            errors.append("XrefService missing native instance-layer lock contract: " + needle)

    lock_pos = text.find("using (document.LockDocument())")
    tr_pos = text.find("StartTransaction()", lock_pos)
    xref_pos = text.find("FindRecord(document.Database, transaction, xrefName)", tr_pos)
    space_pos = text.find("document.Database.CurrentSpaceId", xref_pos)
    collect_pos = text.find("var layerIds = new HashSet<ObjectId>();", space_pos)
    write_pos = text.find("layer.IsLocked = locked;", collect_pos)
    commit_pos = text.find("transaction.Commit();", write_pos)
    if min(lock_pos, tr_pos, xref_pos, space_pos, collect_pos, write_pos, commit_pos) < 0 or not (
        lock_pos < tr_pos < xref_pos < space_pos < collect_pos < write_pos < commit_pos
    ):
        errors.append("Xref layer lock must order document lock -> write transaction -> Xref resolution -> current-space instances -> dedup layers -> writes -> commit")

    method_start = text.find("public static int SetInstanceLayersLocked")
    method_end = text.find("public static void Reload", method_start)
    body = text[method_start:method_end]
    for forbidden in ("ProjectContextCoordinator", "ExistingProjectMutationContext", ".qsdb", "ProjectState", "DetachXref", "ReloadXrefs"):
        if forbidden in body:
            errors.append("Xref layer lock must not mutate semantic/QSDB/Xref source state: " + forbidden)

if XAML.is_file():
    text = XAML.read_text(encoding="utf-8")
    required = (
        'Content="Khóa" Click="OnLockXrefClick"',
        'Content="Mở khóa" Click="OnUnlockXrefClick"',
        'Header="Khóa layer Xref" Click="OnLockXrefClick"',
        'Header="Mở khóa layer Xref" Click="OnUnlockXrefClick"',
        'GridViewColumn Header="Khóa"',
        'Click="OnReloadXrefClick"',
        'Click="OnMoveDrawingClick"',
        'Click="OnDeleteDrawingClick"',
        'Click="OnZoomWindowClick"',
    )
    for needle in required:
        if needle not in text:
            errors.append("RightPanel.xaml missing Xref lock/preserved drawing action contract: " + needle)

if PARTIAL.is_file():
    text = PARTIAL.read_text(encoding="utf-8")
    required = (
        "private void OnLockXrefClick(object sender, RoutedEventArgs e) => SetSelectedXrefInstanceLayerLocks(true);",
        "private void OnUnlockXrefClick(object sender, RoutedEventArgs e) => SetSelectedXrefInstanceLayerLocks(false);",
        "var item = SelectedXref();",
        "XrefService.SetInstanceLayersLocked(document, item.Name, locked);",
        "affected == 0",
        "RefreshAfterXrefMutation(status);",
        "RefreshDrawingsOnly();",
        "ReloadLayers();",
    )
    for needle in required:
        if needle not in text:
            errors.append("RightPanel Xref-lock partial missing handler/refresh contract: " + needle)
    for forbidden in ("ProjectContextCoordinator", "ExistingProjectMutationContext", ".qsdb", "SendStringToExecute"):
        if forbidden in text:
            errors.append("Xref lock handlers must remain native-layer-only and direct, not semantic/command-string mutation: " + forbidden)

if CORE_UI.is_file():
    text = CORE_UI.read_text(encoding="utf-8")
    required = (
        "private DrawingItemViewModel? SelectedXref()",
        "if (!item.IsXref)",
        "private void RefreshAfterXrefMutation(string successStatus)",
        "RefreshDrawingsOnly();",
        "ReloadLayers();",
        "XrefService.SelectInstances(doc, item.Name)",
        "XrefService.Reload(doc, item.Name)",
        "XrefService.Detach(doc, item.Name)",
        "XrefSelectionFailureStatus",
        "XrefReloadFailureStatus",
        "XrefMoveFailureStatus",
        "XrefDetachFailureStatus",
        "RefreshWarningSuffix",
    )
    for needle in required:
        if needle not in text:
            errors.append("existing RightPanel Xref safety/action/redaction contract disappeared: " + needle)
    for forbidden in ("ex.Message", "catch (Exception ex)"):
        if forbidden in text:
            errors.append("RightPanel Xref-facing core UI must not expose raw host exception detail: " + forbidden)

print("QS3D Xref instance-layer lock preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: selected Xref lock/unlock is scoped to deduplicated current-space instance layers, refreshes drawing/layer state, rejects the main DWG through SelectedXref, preserves existing Xref actions, and keeps Xref-facing failure statuses redacted.")