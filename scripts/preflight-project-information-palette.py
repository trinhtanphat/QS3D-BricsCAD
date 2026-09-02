#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/BltProjectSetupPanel.cs"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/ProjectSetupPaletteCoordinator.cs"
panel = PANEL.read_text(encoding="utf-8")
coordinator = COORDINATOR.read_text(encoding="utf-8")

for token in (
    "RefreshFromDocument(Document? document)",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "project.ProjectId",
    "project.DrawingFingerprint",
    "project.ActiveZoneId",
    "project.ActiveFloorId",
    "project.Zones.Count",
    "project.Floors.Count",
    "project.Families.Count",
    "project.Elements.Count",
    "project.QuantityRules.Count",
    "ShowUnavailable(\"Bản vẽ hiện hành chưa có QS3D project.",
    "Dữ liệu cũ đã được xóa",
):
    if token not in panel:
        raise SystemExit(f"Project Information panel contract missing token: {token}")

for forbidden in (
    "(Chưa xây dựng — Thông tin dự án)",
    "ProjectContextCoordinator.GetOrCreate(",
    "ProjectContextCoordinator.Save(",
    ".Touch()",
    "RegenerationEngine",
    "ex.Message",
    "error.Message",
    "Exception.Message",
):
    if forbidden in panel:
        raise SystemExit(f"Project Information panel must remain read-only/redacted: {forbidden}")

for token in (
    "SubscribeToDocumentActivation();",
    "Application.DocumentManager.DocumentActivated += OnDocumentActivated;",
    "Application.DocumentManager.DocumentActivated -= OnDocumentActivated;",
    "panel.RefreshFromDocument(Application.DocumentManager.MdiActiveDocument);",
    "panel.RefreshFromDocument(e.Document ?? Application.DocumentManager.MdiActiveDocument);",
    "finally { UnsubscribeFromDocumentActivation(); }",
    "UnsubscribeFromDocumentActivation();",
    "panel.ShowUnavailable(\"Project Information không thể đọc bản vẽ vừa kích hoạt; dữ liệu cũ đã được xóa.\")",
):
    if token not in coordinator:
        raise SystemExit(f"Project Information palette lifecycle missing token: {token}")

for forbidden in ("ex.Message", "error.Message", "Exception.Message"):
    if forbidden in coordinator:
        raise SystemExit(f"Project Information coordinator must not expose raw exception detail: {forbidden}")

print("PASS Project Information read-only document-bound palette lifecycle")
