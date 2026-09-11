from pathlib import Path

source = Path("src/QS3D.BricsCAD.V25/McpDiagnosticHub.cs").read_text(encoding="utf-8")

def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL: " + message)

for token in (
    "_unhandledExceptionMayBeSubscribed",
    "_unobservedTaskExceptionMayBeSubscribed",
    "_documentBecameCurrentMayBeSubscribed",
    "DetachGlobalSubscriptionsBestEffort",
    "HasGlobalSubscriptionOwnershipLocked"):
    require(token in source, "missing global subscription ownership token: " + token)

start_a = source.find("internal static void Start()")
start_b = source.find("internal static void Stop()", start_a)
require(start_a >= 0 and start_b > start_a, "Start block not found")
start = source[start_a:start_b]

pairs = (
    ("_unhandledExceptionMayBeSubscribed = true", "AppDomain.CurrentDomain.UnhandledException += OnUnhandledException"),
    ("_unobservedTaskExceptionMayBeSubscribed = true", "TaskScheduler.UnobservedTaskException += OnUnobservedTaskException"),
    ("_documentBecameCurrentMayBeSubscribed = true", "Application.DocumentManager.DocumentBecameCurrent += OnDocumentBecameCurrent"),
)
for ownership, attach in pairs:
    require(ownership in start and attach in start, "missing global attach pair: " + attach)
    require(start.find(ownership) < start.find(attach), "ownership must precede fallible global add: " + attach)

require(start.find("_started = true") > start.find("Application.DocumentManager.DocumentBecameCurrent += OnDocumentBecameCurrent"),
        "diagnostics must publish started only after all required global subscriptions attach")
require("HasGlobalSubscriptionOwnershipLocked()" in start,
        "Start must fail closed while unresolved global subscription cleanup is retained")
require("DetachGlobalSubscriptionsBestEffort()" in start,
        "partial global attach failure must attempt bounded cleanup without forgetting ownership")

stop_a = start_b
stop_b = source.find("internal static string InvokeInCadContext", stop_a)
require(stop_b > stop_a, "Stop block not found")
stop = source[stop_a:stop_b]
require("HasGlobalSubscriptionOwnershipLocked()" in stop,
        "Stop early-exit must account for unresolved global subscription ownership")
require("DetachGlobalSubscriptionsBestEffort()" in stop,
        "Stop must use ownership-aware global detach")
require("try { Application.DocumentManager.DocumentBecameCurrent -= OnDocumentBecameCurrent; } catch { }" not in stop,
        "Stop must not blindly swallow global detach failure and forget ownership")

print("PASS: MCP diagnostics retains process/global subscription ownership across partial attach/detach failure")
