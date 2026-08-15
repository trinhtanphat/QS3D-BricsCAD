#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WallQuantityWindow.xaml.cs"

errors = []
try:
    source = WINDOW.read_text(encoding="utf-8")
except Exception as exc:
    print(f"ERROR: cannot read {WINDOW}: {exc}")
    sys.exit(1)

method_name = "private IReadOnlyList<string> Resolve3DLocateHandles("
start = source.find(method_name)
end_marker = "\n        private QuantityReportRow ResolveCurrentRow("
end = source.find(end_marker, start)
if start < 0 or end < 0:
    errors.append("WallQuantityWindow must contain the focused Resolve3DLocateHandles helper")
    body = ""
else:
    body = source[start:end]

locate_start = source.find("private void LocateSelected(")
locate_end = source.find(method_name, locate_start)
locate_body = source[locate_start:locate_end] if locate_start >= 0 and locate_end >= 0 else ""

required_locate = [
    "var currentRow = ResolveCurrentRow(currentProject, displayedView);",
    "var currentElement = currentProject.FindElement(elementId)",
    "var handles = Resolve3DLocateHandles(currentProject, currentElement, currentRow);",
]
for token in required_locate:
    if token not in locate_body:
        errors.append(f"LocateSelected must revalidate and route through 3D handle resolution: missing {token}")

required_body = [
    'const string generatedSolidHandleKey = "GeneratedSolidHandle";',
    "currentElement.Properties.TryGetValue(generatedSolidHandleKey, out var rawGeneratedHandle)",
    "SourceHandleResolver.Resolve(currentProject, currentRow.ElementIds)",
    "currentElement.IsGeneratedSolidStale()",
    "CadHandleService.NormalizeHexHandle(rawGeneratedHandle)",
    "CadHandleService.GetLiveSolidHandles(_document, new[] { normalized })",
    "GeneratedGeometryService.FindMatchingOwnedHandles(",
    "currentProject.ProjectId",
    "currentElement.Id",
    "currentElement.Category",
    "return new[] { normalized };",
]
for token in required_body:
    if token not in body:
        errors.append(f"3D locate contract missing token: {token}")

if body:
    missing_key = body.find("if (!currentElement.Properties.TryGetValue")
    source_fallback = body.find("SourceHandleResolver.Resolve", missing_key)
    missing_key_close = body.find("\n            }", source_fallback)
    stale_check = body.find("currentElement.IsGeneratedSolidStale()")
    if min(missing_key, source_fallback, missing_key_close, stale_check) < 0:
        errors.append("could not prove source fallback is isolated to the no-generated-handle branch")
    elif not (missing_key < source_fallback < missing_key_close < stale_check):
        errors.append("source fallback must occur only when GeneratedSolidHandle is absent")

    if body.count("SourceHandleResolver.Resolve") != 1:
        errors.append("3D locate helper must have exactly one source-handle fallback")
    if "return sourceHandles;" not in body:
        errors.append("missing generated-handle case must return validated source handles")
    if body.count("return new[] { normalized };") != 1:
        errors.append("validated generated-solid path must return exactly the configured generated handle")

    fail_closed_messages = [
        "từ chối fallback sang hình học nguồn",
        "Solid3d generated của Tường đang stale",
    ]
    for token in fail_closed_messages:
        if token not in body:
            errors.append(f"configured invalid/stale generated geometry must fail closed: missing {token}")

for forbidden in ["OpenMode.ForWrite", ".Erase(", ".XData =", "SetProperty(", "MarkGenerated("]:
    if forbidden in body:
        errors.append(f"3D locate resolver must remain read-only; found {forbidden}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS wall quantity 3D locate prefers a live ownership-matched generated Solid3d and only falls back when no generated handle is configured")
