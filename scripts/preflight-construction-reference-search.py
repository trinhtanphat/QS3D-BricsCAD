#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ReferenceSearchCommands.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml.cs"
DOC = ROOT / "docs/CONSTRUCTION-REFERENCE-SEARCH.md"

errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label}: forbidden {token!r}")


for path in (COMMAND, XAML, CODE, DOC):
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")

if errors:
    print("construction reference search preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

command = COMMAND.read_text(encoding="utf-8")
xaml = XAML.read_text(encoding="utf-8")
code = CODE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

require(command, '[CommandMethod("QS3DREFSEARCH", CommandFlags.Modal)]', "command registration")
require(command, "Application.DocumentManager.MdiActiveDocument", "active document lookup")
require(command, "Application.ShowModelessWindow", "modeless launcher")
require(command, "new ReferenceSearchWindow(document)", "document-bound window")

for kind, label in (
    ("images", "Hình ảnh"),
    ("web", "Web"),
    ("video", "Video"),
    ("shopping", "Mua sắm"),
    ("shorts", "Video ngắn"),
    ("news", "Tin tức"),
):
    require(xaml, f'Tag="{kind}"', f"{label} category")

for query in (
    "Ván khuôn móng",
    "Cốt thép móng",
    "Chi tiết dầm",
    "Chi tiết sàn",
    "Cấu tạo tường",
    "Mặt cắt móng",
):
    require(xaml, query, f"quick query {query}")

for handler in ("OnSearchClick", "OnQuickQueryClick", "OnQueryKeyDown"):
    require(xaml, handler, f"XAML handler {handler}")
    require(code, f"{handler}(", f"code-behind handler {handler}")

require(code, "DocumentBoundWindowLifetime.Attach(this, _document)", "window lifetime binding")
require(code, "var active = Application.DocumentManager.MdiActiveDocument;", "active document capture")
require(code, "ReferenceEquals(active, _document)", "exact managed document affinity")
require(code, "var activeIdentity = GetNativeDatabaseIdentity(active);", "native database affinity lookup")
require(code, "activeIdentity != _nativeDatabaseIdentity", "native database affinity rejection")
require(code, "Uri.EscapeDataString", "query encoding")
require(code, "ProcessStartInfo", "browser launcher")
require(code, "UseShellExecute = true", "default browser shell launch")
require(code, "Process.Start(startInfo)", "browser process start")
require(code, '"https://www.google.com/search?', "fixed HTTPS provider")
require(code, "safe=active", "SafeSearch query")
require(code, "MaxQueryLength = 512", "bounded user input")

for forbidden in (
    "HttpClient",
    "WebClient",
    "HttpWebRequest",
    "WebRequest.Create",
    "WebBrowser",
    "Navigate(",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "StartTransaction(",
    "OpenMode.ForWrite",
):
    forbid(command + "\n" + code, forbidden, "no scrape/project mutation boundary")

for forbidden_scheme in ("http://", "file://", "javascript:", "data:"):
    forbid(code, forbidden_scheme, "URL scheme boundary")

try:
    ET.parse(XAML)
except ET.ParseError as exc:
    errors.append(f"ReferenceSearchWindow XAML XML parse failed: {exc}")

require(doc, "QS3DREFSEARCH", "documentation command")
require(doc, "trình duyệt mặc định", "documentation browser behavior")
require(doc, "không scrape", "documentation scrape boundary")

if errors:
    print("construction reference search preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("construction reference search preflight: PASS")
