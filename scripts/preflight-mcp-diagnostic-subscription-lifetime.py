from pathlib import Path

source = Path("src/QS3D.BricsCAD.V25/McpDiagnosticHub.cs").read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL: " + message)

start = source.find("private sealed class DocumentSubscription")
end = source.find("private sealed class CadReadWorkItem", start)
require(start >= 0 and end > start, "DocumentSubscription block not found")
block = source[start:end]

for token in (
    "_willStartMayBeSubscribed", "_endedMayBeSubscribed",
    "_cancelledMayBeSubscribed", "_failedMayBeSubscribed",
    "DetachBestEffort()", "AcceptCallbacks"):
    require(token in block, "missing conservative native subscription ownership token: " + token)

require(block.find("_willStartMayBeSubscribed = true") < block.find("Document.CommandWillStart += WillStart"),
        "WillStart ownership must be retained before fallible native add")
require(block.find("_endedMayBeSubscribed = true") < block.find("Document.CommandEnded += Ended"),
        "Ended ownership must be retained before fallible native add")
require(block.find("_cancelledMayBeSubscribed = true") < block.find("Document.CommandCancelled += Cancelled"),
        "Cancelled ownership must be retained before fallible native add")
require(block.find("_failedMayBeSubscribed = true") < block.find("Document.CommandFailed += Failed"),
        "Failed ownership must be retained before fallible native add")
attach_start = source.find("private static void Attach(Document? document)")
attach_end = source.find("private static void OnCommand", attach_start)
require(attach_start >= 0 and attach_end > attach_start, "Attach block not found")
attach = source[attach_start:attach_end]
require("Subscriptions.Add(document, subscription)" in attach,
        "subscription ownership must be published before native attach can partially fail")
require(attach.find("Subscriptions.Add(document, subscription)") < attach.find("subscription.Subscribe()"),
        "registry ownership must precede fallible Subscribe")
require("subscription.DetachBestEffort()" in attach,
        "partial attach failure must attempt bounded detach without forgetting unresolved ownership")
require("Subscriptions.Remove(document)" in attach,
        "resolved rollback must remove only after detach is proven")

stop_start = source.find("internal static void Stop()")
stop_end = source.find("internal static string InvokeInCadContext", stop_start)
require(stop_start >= 0 and stop_end > stop_start, "Stop block not found")
stop = source[stop_start:stop_end]
require("DetachBestEffort()" in stop, "Stop must use retry-aware native detach")
require("Subscriptions.Clear()" not in stop,
        "Stop must not forget subscriptions before native detach is proven")

require("if (!subscription.AcceptCallbacks) return;" in source,
        "stale/partially attached callbacks must be fail-soft contained")
print("PASS: MCP diagnostics retains native subscription ownership across partial attach/detach failure")