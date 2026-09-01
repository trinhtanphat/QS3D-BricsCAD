#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI"
FILES = (
    ("DomainHubWindow.xaml.cs", "command"),
    ("Rebar3DHubWindow.xaml.cs", "normalizedCommand"),
)
errors = []

for name, command_var in FILES:
    path = UI / name
    if not path.is_file():
        errors.append("missing command hub: " + name)
        continue
    text = path.read_text(encoding="utf-8")
    required = (
        "MdiActiveDocument",
        "SendStringToExecute",
        'StatusText.Text = "Không thể gửi lệnh " + ' + command_var + ' + " sang BricsCAD.";',
        'document.Editor.WriteMessage("\\n" + ' + command_var + ' + " dispatch failed (" + ex.GetType().Name + ").")',
    )
    for token in required:
        if token not in text:
            errors.append(name + " missing redacted dispatch token: " + token)
    for forbidden in ("ex.Message", "Exception.Message", "GetBaseException()", "StackTrace"):
        if forbidden in text:
            errors.append(name + " exposes raw host exception detail: " + forbidden)

    try_at = text.find("try\n            {")
    send_at = text.find("SendStringToExecute", try_at)
    success_at = text.find("Đã gửi lệnh", send_at)
    catch_at = text.find("catch (Exception ex)", success_at)
    failure_at = text.find("sang BricsCAD.", catch_at)
    diagnostic_at = text.find("dispatch failed (", failure_at)
    if min(try_at, send_at, success_at, catch_at, failure_at, diagnostic_at) < 0 or not (
        try_at < send_at < success_at < catch_at < failure_at < diagnostic_at
    ):
        errors.append(name + " must preserve dispatch -> success / catch -> stable failure -> diagnostic ordering")

    diagnostic = text[diagnostic_at:text.find("}", diagnostic_at) + 1]
    if "try" not in text[max(catch_at, 0):diagnostic_at + 200] or "catch { }" not in text[diagnostic_at:diagnostic_at + 240]:
        errors.append(name + " diagnostic write must be best-effort/non-escaping")

if errors:
    print("Dynamic command-hub redaction guard FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Dynamic command-hub redaction guard PASS")
