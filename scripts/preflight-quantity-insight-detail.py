from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI"

files = {
    "registration": UI / "QuantityInsightPanel.DetailExplainer.Registration.cs",
    "data": UI / "QuantityInsightPanel.DetailExplainer.Data.cs",
    "render": UI / "QuantityInsightPanel.DetailExplainer.Render.cs",
    "metrics": UI / "QuantityInsightPanel.DetailExplainer.Metrics.cs",
    "locate": UI / "QuantityInsightPanel.DetailExplainer.Locate.cs",
}

text = {name: path.read_text(encoding="utf-8") for name, path in files.items()}
required = {
    "registration": ["CHI TIẾT", "RefreshQuantityDetail", "SelectedItemChanged"],
    "data": ["ProjectQuantityReportBuilder.Detail", "ProjectStateSnapshot.CreateDetachedCopy", "RegenerateDirty"],
    "render": ["GrossConcreteM3", "DeductionM3", "NetConcreteM3", "FormworkM2", "OuterPerimeterM", "DoorAreaM2", "DensityKgM3", "MassKg"],
    "metrics": ["Bê tông gộp", "Trừ giao cắt", "Chu vi ngoài", "Diện tích cửa", "Khối lượng riêng"],
    "locate": ["SameRow", "SameElementIdentity", "SourceHandleResolver.Resolve", "CadHandleService.Select", "ViewportCommands.TryZoomSelection(document)"],
}

failures = []
for name, tokens in required.items():
    for token in tokens:
        if token not in text[name]:
            failures.append(f"{name}: missing {token!r}")

joined = "\n".join(text.values())
for forbidden in ["ProjectContextCoordinator.Edit", "SaveProject(", "ProjectStateStore.Save"]:
    if forbidden in joined:
        failures.append(f"detail explainer must remain read-only: found {forbidden!r}")
if 'SendStringToExecute("QS3DZOOMSELECTED ' in text["locate"]:
    failures.append("detail locate must use direct in-process zoom instead of queued command re-entry")

if failures:
    print("Quantity Insight detail preflight FAILED:")
    for failure in failures:
        print(" -", failure)
    raise SystemExit(1)

print("Quantity Insight detail preflight PASS")
