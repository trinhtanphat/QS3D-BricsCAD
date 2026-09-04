from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/McpDiagnosticHub.cs"
text = SOURCE.read_text(encoding="utf-8")


def method_body(name: str) -> str:
    method = re.search(
        rf"(?m)^\s*(?:(?:private|internal|public|protected)\s+)?static\s+void\s+{re.escape(name)}\s*\(",
        text,
    )
    if not method:
        raise SystemExit(f"FAIL: {name} method not found")
    start = method.start()
    brace = text.find("{", start)
    if brace < 0:
        raise SystemExit(f"FAIL: {name} body not found")
    depth = 0
    for index in range(brace, len(text)):
        ch = text[index]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    raise SystemExit(f"FAIL: {name} body is unbalanced")


start_body = method_body("Start")
stop_body = method_body("Stop")
poll_body = method_body("Poll")

# The 1-second diagnostics timer may inspect bounded transport/OAuth state, but it must
# never enqueue a BricsCAD application-context callback on every idle tick. Repeated
# ExecuteInApplicationContext hops can serialize/wake the host while the user is simply
# editing or idling in an already-attached drawing.
if "QueueAttachActiveDocument();" in poll_body:
    raise SystemExit("FAIL: 1-second diagnostics Poll still enqueues CAD-context attachment work")

# Startup still needs one initial attachment attempt for the already-current document.
if "QueueAttachActiveDocument();" not in start_body:
    raise SystemExit("FAIL: diagnostics startup no longer schedules initial active-document attachment")

# Subsequent drawing switches must be demand-driven by host document lifecycle instead
# of timer polling. DocumentBecameCurrent is a BricsCAD DocumentCollection event and the
# existing Attach() dictionary makes duplicate notifications harmless.
required_start = "Application.DocumentManager.DocumentBecameCurrent += OnDocumentBecameCurrent;"
required_stop = "Application.DocumentManager.DocumentBecameCurrent -= OnDocumentBecameCurrent;"
if required_start not in start_body:
    raise SystemExit("FAIL: diagnostics does not subscribe to DocumentBecameCurrent")
if required_stop not in stop_body:
    raise SystemExit("FAIL: diagnostics does not unsubscribe from DocumentBecameCurrent")

handler = re.search(
    r"private static void OnDocumentBecameCurrent\(object sender, DocumentCollectionEventArgs e\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not handler:
    raise SystemExit("FAIL: DocumentBecameCurrent handler not found")
handler_body = handler.group("body")
if "QueueAttachActiveDocument();" not in handler_body and "Attach(e.Document);" not in handler_body:
    raise SystemExit("FAIL: DocumentBecameCurrent no longer triggers command-monitor attachment")

# Keep the original bounded diagnostics cadence and transport/OAuth checks; only the
# unnecessary CAD-context hop is removed from the idle path.
for token in [
    "_pollTimer = new Timer(Poll, null, 750, 1000);",
    "McpEmbeddedServer.LastError",
    "McpEmbeddedServer.LastOAuthMcpActivityUtc",
]:
    if token not in text:
        raise SystemExit(f"FAIL: existing bounded diagnostics contract disappeared: {token}")

print("PASS: diagnostics idle polling no longer hops into CAD context; document switches attach on demand")
