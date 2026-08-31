#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml.cs"
errors = []

if not CODE.is_file():
    errors.append("missing ReferenceSearchWindow.xaml.cs")
else:
    text = CODE.read_text(encoding="utf-8")

    required = (
        "private const int MaxQueryLength = 512;",
        "DocumentBoundWindowLifetime.Attach(this, _document)",
        "var active = Application.DocumentManager.MdiActiveDocument;",
        "ReferenceEquals(active, _document)",
        "var activeIdentity = GetNativeDatabaseIdentity(active);",
        "activeIdentity != _nativeDatabaseIdentity",
        "private static string AppendBoundedSuffix(string query, string suffix, string context)",
        "query.Length + suffix.Length > MaxQueryLength",
        'AppendBoundedSuffix(query, " kỹ thuật xây dựng chi tiết thi công", "ngữ cảnh kỹ thuật")',
        'AppendBoundedSuffix(query, " video ngắn shorts", "ngữ cảnh video ngắn")',
        "Uri.EscapeDataString(effectiveQuery)",
        '"https://www.google.com/search?tbm=vid&safe=active&q=" + encoded',
    )
    for token in required:
        if token not in text:
            errors.append("missing effective-query boundary token: " + token)

    forbidden = (
        'query + " video ngắn shorts"',
        'effectiveQuery + " video ngắn shorts"',
    )
    for token in forbidden:
        if token in text:
            errors.append("shorts suffix bypasses bounded append helper: " + token)

    active_capture = text.find("var active = Application.DocumentManager.MdiActiveDocument;")
    wrapper_guard = text.find("ReferenceEquals(active, _document)", active_capture)
    native_lookup = text.find("var activeIdentity = GetNativeDatabaseIdentity(active);", wrapper_guard)
    native_guard = text.find("activeIdentity != _nativeDatabaseIdentity", native_lookup)
    if min(active_capture, wrapper_guard, native_lookup, native_guard) < 0 or not active_capture < wrapper_guard < native_lookup < native_guard:
        errors.append("browser launch affinity must reject managed-wrapper mismatch before native database drift")

    helper_start = text.find("private static string AppendBoundedSuffix")
    build_start = text.find("private static string BuildSearchUrl")
    kind_start = text.find("private static string KindLabel", build_start)
    helper_body = text[helper_start:build_start] if helper_start >= 0 and build_start > helper_start else ""
    build_body = text[build_start:kind_start] if build_start >= 0 and kind_start > build_start else ""

    guard = helper_body.find("query.Length + suffix.Length > MaxQueryLength")
    append = helper_body.find("return query + suffix;")
    if min(guard, append) < 0 or not guard < append:
        errors.append("bounded suffix helper must reject overflow before concatenation")

    shorts_bound = build_body.find('AppendBoundedSuffix(query, " video ngắn shorts", "ngữ cảnh video ngắn")')
    encode = build_body.find("Uri.EscapeDataString(effectiveQuery)")
    if min(shorts_bound, encode) < 0 or not shorts_bound < encode:
        errors.append("shorts effective query must be bounded before URL encoding")

    for unsafe in ("HttpClient", "WebClient", "HttpWebRequest", "WebRequest.Create", "http://", "file://", "javascript:", "data:"):
        if unsafe in text:
            errors.append("reference search boundary regressed: " + unsafe)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] construction reference search bounds raw/effective queries and preserves exact wrapper/native affinity before URL encoding")
